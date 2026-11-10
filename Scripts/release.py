#!/usr/bin/env python3
"""
Release Script for Magnetar Client.

Steps:
  1. Read version from MagnetarInfo.cs
  2. Verify all build outputs exist in Builds/Outputs/
  3. Compute SHA-256 checksums for every zip
  4. Create a 'Version X.X.X' subfolder in the Drive 'Magnetar Client' folder
  5. Upload all zips to Drive and get shareable links
  6. Build the full release body from Changelog.md + Downloads table
  7. Create a draft GitHub release (tag vX.X.X) with the generated body
  8. Upload all artifacts to the draft release

Usage:
  Release.bat                    # full run
  Release.bat --skip-drive       # skip Drive upload (GitHub only)
  Release.bat --skip-github      # skip GitHub release (Drive only)
  Release.bat --dry-run          # print what would happen, do nothing
"""

import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
SCRIPT_DIR = Path(__file__).resolve().parent
ROOT_DIR = SCRIPT_DIR.parent
BUILDS_DIR = ROOT_DIR / "Builds"
OUTPUTS_DIR = BUILDS_DIR / "Outputs"

# Google Drive — parent "Magnetar Client" folder ID
DRIVE_PARENT_FOLDER_ID = "1SkBpqiq6wF5U7H7EdmwF9ZCK-3TI2P0P"

# GitHub repo
GITHUB_REPO = "Tproplay/Magnetar-Client"

# Changelog source (user edits this before running)
CHANGELOG_FILE = ROOT_DIR / "docs" / "nightly_changelog.md"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def log(msg: str):
    print(f"[RELEASE] {msg}")


def log_err(msg: str):
    print(f"[ERROR]   {msg}", file=sys.stderr)


def die(msg: str):
    log_err(msg)
    sys.exit(1)


def run(cmd: list, capture: bool = False, check: bool = True):
    """Run a subprocess, optionally capturing stdout."""
    log(f"Running: {' '.join(str(c) for c in cmd)}")
    if capture:
        result = subprocess.run(cmd, capture_output=True, text=True)
        if check and result.returncode != 0:
            die(f"Command failed (exit {result.returncode}):\n{result.stderr.strip()}")
        return result
    else:
        result = subprocess.run(cmd)
        if check and result.returncode != 0:
            die(f"Command failed with exit code {result.returncode}")
        return result


GDRIVE_BIN = shutil.which("gdrive") or "gdrive"


def gdrive(*args, capture: bool = True) -> subprocess.CompletedProcess:
    return run([GDRIVE_BIN] + list(args), capture=capture, check=True)


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def human_size(path: Path) -> str:
    size = path.stat().st_size
    for unit in ("B", "KB", "MB", "GB"):
        if size < 1024:
            return f"{size:.2f} {unit}"
        size /= 1024
    return f"{size:.2f} GB"


def get_current_branch() -> str:
    try:
        res = subprocess.run(["git", "rev-parse", "--abbrev-ref", "HEAD"], capture_output=True, text=True)
        if res.returncode == 0 and res.stdout.strip():
            return res.stdout.strip()
    except Exception:
        pass
    return "devlop"


# ---------------------------------------------------------------------------
# Version
# ---------------------------------------------------------------------------

def get_version() -> str:
    info_file = ROOT_DIR / "Magnetar Client" / "src" / "Config" / "MagnetarInfo.cs"
    if not info_file.exists():
        die(f"MagnetarInfo.cs not found at {info_file}")
    content = info_file.read_text(encoding="utf-8")
    m = re.search(r'public\s+const\s+string\s+Version\s*=\s*"([^"]+)";', content)
    if not m:
        die("Could not extract Version from MagnetarInfo.cs")
    return m.group(1).strip()


# ---------------------------------------------------------------------------
# Output files
# ---------------------------------------------------------------------------

def get_output_files(version: str) -> list[Path]:
    """Return the expected list of release artifact paths in order."""
    names = [
        f"MagnetarClient-{version}-Melonloader.zip",
        f"MagnetarClient-{version}-Melonloader-FULL.zip",
        f"MagnetarClient-{version}-Bepinex.zip",
        f"MagnetarClient-{version}-Bepinex-FULL.zip",
        f"MagnetarClient-{version}-Android.zip",
        f"MagnetarClient-{version}-Android-FULL.zip",
        f"MagnetarClient-{version}-Addons.zip",
        "Magnetar.Client.xml",
    ]
    return [OUTPUTS_DIR / n for n in names]


def verify_outputs(files: list[Path]):
    missing = [f for f in files if not f.exists()]
    if missing:
        die(
            "Missing build outputs — run Build_All.bat first:\n"
            + "\n".join(f"  {f.name}" for f in missing)
        )
    log(f"All {len(files)} output files verified.")


# ---------------------------------------------------------------------------
# Google Drive
# ---------------------------------------------------------------------------

def drive_mkdir(name: str, parent_id: str) -> str:
    """Create a folder on Drive and return its ID."""
    result = gdrive("mutate", "mkdir", name, "--parent", parent_id)
    # output: "Directory <NAME> created with id: <ID>" or "Created folder: <NAME> (ID: <ID>)"
    m = re.search(r"ID:\s*([A-Za-z0-9_-]+)", result.stdout, re.IGNORECASE)
    if not m:
        die(f"Could not parse folder ID from gdrive output:\n{result.stdout}")
    return m.group(1).strip()


def drive_upload(local_path: Path, parent_id: str) -> str:
    """Upload a file to Drive and return its file ID."""
    result = gdrive("mutate", "upload", str(local_path), "--parent", parent_id)
    # output: "File <NAME> created with id: <ID>" or "Uploaded: <NAME> (ID: <ID>)"
    m = re.search(r"ID:\s*([A-Za-z0-9_-]+)", result.stdout, re.IGNORECASE)
    if not m:
        die(f"Could not parse file ID from gdrive output:\n{result.stdout}")
    return m.group(1).strip()


def drive_share_public(file_id: str):
    """Make a Drive file readable by anyone with the link."""
    gdrive("mutate", "share", file_id, "--type", "anyone", "--role", "reader")


def drive_get_link(file_id: str) -> str:
    return f"https://drive.google.com/file/d/{file_id}/view?usp=sharing"


def upload_to_drive(version: str, files: list[Path], dry_run: bool) -> dict[str, str]:
    """
    Upload all files to Drive under 'Magnetar Client/Version X.X.X/'.
    Returns a dict mapping filename -> shareable Drive URL.
    """
    folder_name = f"Version {version}"
    log(f"Creating Drive folder '{folder_name}' under Magnetar Client...")

    if dry_run:
        log("[DRY RUN] Would create Drive folder and upload all files.")
        return {f.name: f"https://drive.google.com/file/d/DRY_RUN_{f.stem}/view?usp=sharing" for f in files}

    folder_id = drive_mkdir(folder_name, DRIVE_PARENT_FOLDER_ID)
    log(f"Created Drive folder with ID: {folder_id}")

    drive_links: dict[str, str] = {}
    for f in files:
        log(f"Uploading {f.name} to Drive...")
        file_id = drive_upload(f, folder_id)
        drive_share_public(file_id)
        link = drive_get_link(file_id)
        drive_links[f.name] = link
        log(f"  → {link}")

    return drive_links


# ---------------------------------------------------------------------------
# Release body builder
# ---------------------------------------------------------------------------

DRIVE_LABELS = {
    "Melonloader.zip":      ("PC MelonLoader",           "🖥️ **PC Version (MelonLoader ZIP)**"),
    "Melonloader-FULL.zip": ("PC MelonLoader (Full)",    "🖥️ **PC Version (MelonLoader Full ZIP)**"),
    "Bepinex.zip":          ("PC BepInEx",               "🖥️ **PC Version (BepInEx ZIP)**"),
    "Bepinex-FULL.zip":     ("PC BepInEx (Full)",        "🖥️ **PC Version (BepInEx Full ZIP)**"),
    "Android.zip":          ("Android",                  "📱 **Android Version (ZIP)**"),
    "Android-FULL.zip":     ("Android (Full)",           "📱 **Android Version (Full ZIP)**"),
    "Addons.zip":           ("Addons Bundle",            "🧩 **Addons Bundle (ZIP)**"),
}

# Which zips appear in the Downloads table (not XML)
TABLE_ZIPS_SUFFIXES = list(DRIVE_LABELS.keys())

# Which zips get a Drive mirror bullet under the alternative section
MIRROR_BULLET_SUFFIXES = list(DRIVE_LABELS.keys())


def build_release_body(version: str, checksums: dict[str, str], drive_links: dict[str, str]) -> str:
    changelog = CHANGELOG_FILE.read_text(encoding="utf-8").strip()

    # Build Downloads table rows
    table_rows = []
    for suffix, (label, _) in DRIVE_LABELS.items():
        if suffix not in TABLE_ZIPS_SUFFIXES:
            continue
        filename = f"MagnetarClient-{version}-{suffix}"
        gh_url = f"https://github.com/{GITHUB_REPO}/releases/download/v{version}/{filename}"
        chk = checksums.get(filename, "N/A")
        size = ""
        f = OUTPUTS_DIR / filename
        if f.exists():
            size = human_size(f)
        drive_url = drive_links.get(filename, "")
        drive_cell = f"[{label} Drive Mirror]({drive_url})" if drive_url else "N/A"
        table_rows.append(
            f"| **{label}** | [`{filename}`]({gh_url}) | {size} | `{chk}` | {drive_cell} |"
        )

    table = "\n".join([
        "| Platform | File | Size | SHA-256 Checksum | Mirror (Google Drive) |",
        "| :--- | :--- | :--- | :--- | :--- |",
    ] + table_rows)

    # Mirror bullets
    mirror_bullets = []
    for suffix, (_, bullet_label) in DRIVE_LABELS.items():
        if suffix not in MIRROR_BULLET_SUFFIXES or bullet_label is None:
            continue
        filename = f"MagnetarClient-{version}-{suffix}"
        drive_url = drive_links.get(filename, "")
        if drive_url:
            mirror_bullets.append(f"- {bullet_label}: [Google Drive Download]({drive_url})")

    mirror_section = "\n".join(mirror_bullets)

    body = f"""# Magnetar Client v{version}

{changelog}

## Downloads

{table}

### 🌐 Alternative Download Mirror (Google Drive)
If you experience slow download speeds or issues downloading from GitHub Releases, download directly from Google Drive:
{mirror_section}
"""
    return body


# ---------------------------------------------------------------------------
# GitHub Release
# ---------------------------------------------------------------------------

def check_gh_auth(dry_run: bool = False):
    result = subprocess.run(["gh", "auth", "status"], capture_output=True, text=True)
    if result.returncode != 0:
        if dry_run:
            log("[DRY RUN] Warning: GitHub CLI is not authenticated. Proceeding with dry-run...")
            return
        die(
            "GitHub CLI is not authenticated.\n"
            "Run:  gh auth login\n"
            "Then re-run this script."
        )
    log("GitHub CLI authenticated.")


def create_github_draft(version: str, body: str, files: list[Path], target_branch: str, dry_run: bool):
    tag = f"v{version}"
    title = f"Magnetar Client v{version}"
    body_file = OUTPUTS_DIR / "_release_body.md"
    body_file.write_text(body, encoding="utf-8")

    if dry_run:
        log(f"[DRY RUN] Would create draft GitHub release '{title}' (tag {tag}, target {target_branch})")
        log(f"[DRY RUN] Release body preview written to: {body_file}")
        log(f"[DRY RUN] Would upload {len(files)} artifacts.")
        return

    log(f"Creating draft GitHub release '{title}' (tag {tag}, target {target_branch})...")
    run([
        "gh", "release", "create", tag,
        "--repo", GITHUB_REPO,
        "--target", target_branch,
        "--title", title,
        "--notes-file", str(body_file),
        "--draft",
    ])

    log("Uploading artifacts to GitHub draft release...")
    run([
        "gh", "release", "upload", tag,
        "--repo", GITHUB_REPO,
        "--clobber",
    ] + [str(f) for f in files])

    log(f"Draft release created: https://github.com/{GITHUB_REPO}/releases")


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def parse_args():
    args = sys.argv[1:]
    target_branch = None
    for i, a in enumerate(args):
        if a == "--target" and i + 1 < len(args):
            target_branch = args[i + 1]
    if not target_branch:
        target_branch = get_current_branch()

    return {
        "skip_drive":     "--skip-drive"  in args,
        "skip_github":    "--skip-github" in args,
        "dry_run":        "--dry-run"     in args,
        "target_branch":  target_branch,
    }


def main():
    opts = parse_args()

    print()
    print("=" * 60)
    print("  Magnetar Client Release Pipeline")
    print("=" * 60)

    if opts["dry_run"]:
        log("DRY RUN mode — no files will be created or uploaded.")

    # 1. Version
    version = get_version()
    log(f"Version: {version}")

    # 2. Verify outputs
    files = get_output_files(version)
    verify_outputs(files)

    # Zips only (no XML for Drive; XML goes to GitHub only)
    zip_files = [f for f in files if f.suffix == ".zip"]
    all_artifacts = files  # all go to GitHub (including XML)

    # 3. Checksums
    log("Computing SHA-256 checksums...")
    checksums: dict[str, str] = {}
    for f in zip_files:
        chk = sha256(f)
        checksums[f.name] = chk
        log(f"  {f.name}: {chk[:16]}...")

    # 4. Drive upload
    drive_links: dict[str, str] = {}
    if not opts["skip_drive"]:
        drive_links = upload_to_drive(version, zip_files, dry_run=opts["dry_run"])
    else:
        log("Skipping Drive upload (--skip-drive).")

    # 5. Build release body
    log("Building release body...")
    body = build_release_body(version, checksums, drive_links)

    # 6 & 7. GitHub release
    if not opts["skip_github"]:
        check_gh_auth(dry_run=opts["dry_run"])
        create_github_draft(version, body, all_artifacts, target_branch=opts["target_branch"], dry_run=opts["dry_run"])
    else:
        log("Skipping GitHub release (--skip-github).")
        # Still write body for review
        body_file = OUTPUTS_DIR / "_release_body.md"
        body_file.write_text(body, encoding="utf-8")
        log(f"Release body written to: {body_file}")

    print()
    print("=" * 60)
    print("  Release Pipeline Complete!")
    if not opts["dry_run"] and not opts["skip_github"]:
        print(f"  Review and publish: https://github.com/{GITHUB_REPO}/releases")
    print("=" * 60)


if __name__ == "__main__":
    main()
