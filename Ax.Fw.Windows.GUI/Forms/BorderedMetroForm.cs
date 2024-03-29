﻿using MetroFramework;
using MetroFramework.Components;
using MetroFramework.Drawing;
using MetroFramework.Forms;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Forms
{
    public class BorderedMetroForm : MetroForm
    {
        private readonly MetroStyleManager p_styleManager;

        public BorderedMetroForm()
        {
            ShadowType = ShadowType.None;
            p_styleManager = new MetroStyleManager(this) { Style = MetroColorStyle.Blue, Theme = MetroThemeStyle.Light };
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (p_styleManager != null)
            {
                p_styleManager.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (SolidBrush styleBrush = MetroPaint.GetStyleBrush(Style))
            {
                e.Graphics.FillRectangles(styleBrush, new[]
                {
                    new Rectangle(Width - 2, 0, 2, Height), // right
                    new Rectangle(0, 0, 2, Height),         // left
                    new Rectangle(0, Height - 2, Width, 2)  // bottom
                });
            }
        }

        public void PostInvoke(Action action)
        {
            BeginInvoke(new MethodInvoker(action));
        }
    }
}