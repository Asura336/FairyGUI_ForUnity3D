using FairyGUI.Foundations.Collections;
using FairyGUI.Utils;
using UnityEngine;

public class TestUBB : MonoBehaviour
{
    [TextArea]
    public string text;

    [TextArea]
    public string dst;

    private void Start()
    {
        // 提前触发 jit

        using var dict = StringBuilderHandle.New();
        _ = new UBBParser1("aaas");
    }

    [ContextMenu("Run")]
    void Run()
    {
        if (string.IsNullOrEmpty(text)) { return; }
        using var sb = StringBuilderHandle.New(text);
        using var buffer = ArrayHandle<char>.New(text.Length);
        var up = new UBBParser1(text);

        using var dst = StringBuilderHandle.New();
        up.Parse(dst);
        this.dst = dst.ToString();
    }
}
