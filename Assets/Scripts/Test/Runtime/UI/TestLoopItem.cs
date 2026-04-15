using TMPro;
using UniVue.UI.Widegts;

namespace Game
{
    public class TestLoopItem : LoopItem
    {
        public TMP_Text text;

        protected override void OnRenderItem(int index)
        {
            text.text = $"{index + 1}";
        }
    }
}