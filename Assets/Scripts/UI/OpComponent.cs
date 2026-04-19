using System;
using UnityEngine.UI;
using UniVue.Model;
using UniVue.UI;

public sealed partial class OpComponent : BaseComponent
{
    public void BindOp(OpCode opcode, Action<OpCode> codeExecutor)
    {
        Bind(true, () => { TitleTxt.text = opcode.Name; });
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
    private TMPro.TMP_InputField _CodeInput;
    public TMPro.TMP_InputField CodeInput => _CodeInput ? _CodeInput : (_CodeInput = FindByPath($"{UI.name}/CodeInput")?.GetComponent<TMPro.TMP_InputField>());

    private TMPro.TextMeshProUGUI _Text;
    public TMPro.TextMeshProUGUI Text => _Text ? _Text : (_Text = FindByPath($"{UI.name}/CodeInput/Text Area/Text")?.GetComponent<TMPro.TextMeshProUGUI>());

    private UnityEngine.UI.Button _ExeBtn;
    public UnityEngine.UI.Button ExeBtn => _ExeBtn ? _ExeBtn : (_ExeBtn = FindByPath($"{UI.name}/ExeBtn")?.GetComponent<UnityEngine.UI.Button>());

    private TMPro.TextMeshProUGUI _TitleTxt;
    public TMPro.TextMeshProUGUI TitleTxt => _TitleTxt ? _TitleTxt : (_TitleTxt = FindByPath($"{UI.name}/TitleTxt")?.GetComponent<TMPro.TextMeshProUGUI>());
}
#endregion // UniVue Auto-Generated
