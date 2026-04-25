using System;
using UnityEngine.UI;
using UniVue.Model;
using UniVue.UI;

public sealed partial class OpComponent : BaseComponent
{
    public void BindOp(OpCode opcode, Action<OpCode> codeExecutor)
    {
        Bind(opcode, () => { TitleTxt.text = opcode.Name; });
        ExeBtn.onClick.AddListener(() => codeExecutor.Invoke(opcode));
        CodeInput.onEndEdit.AddListener(code => opcode.Code = code);
    }

    public sealed class OpCode : BaseModel
    {
        public string Name { get; set; }

        public string Code { get; set; }
    }
}

#region UniVue Auto-Generated — DO NOT MODIFY
partial class OpComponent
{
    [UniVue.UI.LazyInitUI("/CodeInput")]
    public TMPro.TMP_InputField CodeInput { get; }

    [UniVue.UI.LazyInitUI("/CodeInput/Text Area/Text")]
    public TMPro.TextMeshProUGUI Text { get; }

    [UniVue.UI.LazyInitUI("/ExeBtn")]
    public UnityEngine.UI.Button ExeBtn { get; }

    [UniVue.UI.LazyInitUI("/TitleTxt")]
    public TMPro.TextMeshProUGUI TitleTxt { get; }
}
#endregion // UniVue Auto-Generated
