#if MELONLOADER || RELEASE_MELON
using Il2Cpp;
#endif

namespace Magnetar_Client.Modules
{
    public class RandomCardRoulette : Module
    {
        public override string Name { get; set; } = "Card Roulette";
        public override string Description { get; set; } = "Scrambles your seed bank every frame with 0-cost, instant-cooldown random plants.";
        public override string SearchHints { get; set; } = "cardroulette seedbank scramble shuffle randomplants randomseed " +
            "seedroulette frameplant framechange zerocost freeplants freecards instantcooldown nocoldown instantplants " +
            "cardscramble roudlette seedrandom cardshuffle plantroulette cardscrambler seedscrambler instantseed framecooldown " +
            "freeluck costzero randomcard instantcard luckseed";
        public override ModuleCategory Category { get; set; } = ModuleCategory.Level;

        public override void OnUpdateActive()
        {
            if (Game.AppData.BoardInstanceIsNull || InGameUI.Instance == null) return;

            var randomPlant = GameAPP.resourcesManager.allPlants;
            
            if (randomPlant != null && randomPlant.Count != 0)
            {
                for (int i = 0; i < InGameUI.Instance.Cards.Count; i++)
                {
                    try
                    {
                        var index = UnityEngine.Random.RandomRangeInt(0, randomPlant.Count);
                        var card = InGameUI.Instance.Cards[i];

                        card.thePlantType = randomPlant[index];
                        card.ChangeCardSprite();
                        card.theSeedCost = 0;
                        card.fullCD = 0;
                    }
                    catch { }
                }
            }
        }
    }
}