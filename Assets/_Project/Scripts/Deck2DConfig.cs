using UnityEngine;

namespace TPD
{
    [CreateAssetMenu(menuName = "TPD/DeckConfig", fileName = "Deck2DConfig")]
    public class Deck2DConfig : ScriptableObject
    {
        public Sprite[] cardFronts = new Sprite[52];
    }
}
