using System;
using System.Linq;
using UnityEngine;

namespace FairyGUI.Extensions
{
    public static class StageCursorsRegister
    {
        const string ROOT = "FGUICursors/";
        const string intro = "cursordefs";

        /// <summary>
        /// 找到脚本同级的 Resources 文件夹，里面是常用光标图片和中心点配置
        /// </summary>
        public static void RegistCursors()
        {
            var introFile = Resources.Load<TextAsset>(ROOT + intro);
            var stage = Stage.inst;
            foreach (var line in introFile.text.Split('\r', '\n')
                .Where(c => !string.IsNullOrEmpty(c)))
            {
                // {cursor name},{png file name},{hotspot x},{hotspot y}
                var group = line.Split(',', 4);
                var name = group[0];
                var texture = Resources.Load<Texture2D>(ROOT + group[1]);
                var hotspot = new Vector2(Convert.ToSingle(group[2]), Convert.ToSingle(group[3]));

                stage.RegisterCursor(name, texture, hotspot);
            }
        }
    }
}