﻿using MetroFramework.Components;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Helpers
{
    public class ErrorProviderExt
    {
        private static readonly Dictionary<Control, Color> ControlColors = new Dictionary<Control, Color>();
        private static readonly MetroToolTip ToolTip = new MetroToolTip();

        public static void SetError(Control control, string text, Color color)
        {
            if (!ControlColors.ContainsKey(control))
            {
                ControlColors.Add(control, control.BackColor);
                control.LostFocus += ControlOnLostFocus;
                control.Disposed += ControlOnDisposed;
            }
            control.BackColor = color;
            ToolTip.Show(text, control, control.Width, control.Height);
        }

        public static void ClearError(Control control)
        {
            if (ControlColors.ContainsKey(control))
            {
                control.BackColor = ControlColors[control];
                ToolTip.Hide(control);
                ControlColors.Remove(control);
                control.LostFocus -= ControlOnLostFocus;
                control.Disposed -= ControlOnDisposed;
            }
        }

        private static void ControlOnLostFocus(object sender, EventArgs eventArgs)
        {
            if (sender is Control control)
            {
                ToolTip.Hide(control);
            }
        }

        private static void ControlOnDisposed(object sender, EventArgs eventArgs)
        {
            if (sender is Control control)
            {
                ControlColors.Remove(control);
                control.LostFocus -= ControlOnLostFocus;
                control.Disposed -= ControlOnDisposed;
            }
        }
    }
}