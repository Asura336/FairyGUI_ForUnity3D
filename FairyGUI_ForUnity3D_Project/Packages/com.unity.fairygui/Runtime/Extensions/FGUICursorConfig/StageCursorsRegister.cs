using System;
using System.Linq;
using UnityEngine;

namespace FairyGUI.Extensions
{
    static class StageCursorsRegister
    {
        const string ROOT = "FGUICursors/";
        const string intro = "cursordefs";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RegistCursors()
        {
            var introFile = Resources.Load<TextAsset>(ROOT + intro);
            var stage = Stage.inst;
            foreach (var line in introFile.text.Split('\r', '\n').Where(c => !string.IsNullOrEmpty(c)))
            {
                var group = line.Split(',');
                var name = group[0];
                var texture = Resources.Load<Texture2D>(ROOT + group[1]);
                var hotspot = new Vector2(Convert.ToSingle(group[2]), Convert.ToSingle(group[3]));

                stage.RegisterCursor(name, texture, hotspot);
            }
        }
    }
}