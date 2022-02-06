using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Ax.Fw.Windows.GUI.Forms
{
    public class MetroStyleManager : Component, ICloneable
    {
        private Form ownerForm;

        private MetroColorStyle metroStyle = MetroColorStyle.Blue;

        private MetroThemeStyle metroTheme;

        public Form OwnerForm
        {
            get
            {
                return ownerForm;
            }
            set
            {
                if (ownerForm == null)
                {
                    ownerForm = value;
                    ownerForm.ControlAdded += NewControlOnOwnerForm;
                    UpdateOwnerForm();
                }
            }
        }

        public MetroColorStyle Style
        {
            get
            {
                return metroStyle;
            }
            set
            {
                metroStyle = value;
                UpdateOwnerForm();
            }
        }

        public MetroThemeStyle Theme
        {
            get
            {
                return metroTheme;
            }
            set
            {
                metroTheme = value;
                UpdateOwnerForm();
            }
        }

        public MetroStyleManager()
        {
        }

        public MetroStyleManager(Form ownerForm)
        {
            OwnerForm = ownerForm;
        }

        private void NewControlOnOwnerForm(object sender, ControlEventArgs e)
        {
            if (e.Control is IMetroControl)
            {
                ((IMetroControl)e.Control).Style = Style;
                ((IMetroControl)e.Control).Theme = Theme;
                ((IMetroControl)e.Control).StyleManager = this;
            }
            else if (e.Control is IMetroComponent)
            {
                ((IMetroComponent)e.Control).Style = Style;
                ((IMetroComponent)e.Control).Theme = Theme;
                ((IMetroComponent)e.Control).StyleManager = this;
            }
            else
            {
                UpdateOwnerForm();
            }
        }

        public void UpdateOwnerForm()
        {
            if (ownerForm != null)
            {
                if (ownerForm is IMetroForm)
                {
                    ((IMetroForm)ownerForm).Style = Style;
                    ((IMetroForm)ownerForm).Theme = Theme;
                    ((IMetroForm)ownerForm).StyleManager = this;
                }

                if (ownerForm.Controls.Count > 0)
                {
                    UpdateControlCollection(ownerForm.Controls);
                }

                if (ownerForm.ContextMenuStrip != null && ownerForm.ContextMenuStrip is IMetroComponent)
                {
                    ((IMetroComponent)ownerForm.ContextMenuStrip).Style = Style;
                    ((IMetroComponent)ownerForm.ContextMenuStrip).Theme = Theme;
                    ((IMetroComponent)ownerForm.ContextMenuStrip).StyleManager = this;
                }

                ownerForm.Refresh();
            }
        }

        private void UpdateControlCollection(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is IMetroControl)
                {
                    ((IMetroControl)control).Style = Style;
                    ((IMetroControl)control).Theme = Theme;
                    ((IMetroControl)control).StyleManager = this;
                }

                if (control.ContextMenuStrip != null && control.ContextMenuStrip is IMetroComponent)
                {
                    ((IMetroComponent)control.ContextMenuStrip).Style = Style;
                    ((IMetroComponent)control.ContextMenuStrip).Theme = Theme;
                    ((IMetroComponent)control.ContextMenuStrip).StyleManager = this;
                }
                else if (control is IMetroComponent)
                {
                    ((IMetroComponent)control.ContextMenuStrip).Style = Style;
                    ((IMetroComponent)control.ContextMenuStrip).Theme = Theme;
                    ((IMetroComponent)control.ContextMenuStrip).StyleManager = this;
                }

                if (control is TabControl)
                {
                    foreach (TabPage tabPage in ((TabControl)control).TabPages)
                    {
                        if (tabPage is IMetroControl)
                        {
                            ((IMetroControl)control).Style = Style;
                            ((IMetroControl)control).Theme = Theme;
                            ((IMetroControl)control).StyleManager = this;
                        }

                        if (tabPage.Controls.Count > 0)
                        {
                            UpdateControlCollection(tabPage.Controls);
                        }
                    }
                }

                if (control is Panel || control is GroupBox || control is ContainerControl)
                {
                    UpdateControlCollection(control.Controls);
                }
                else if (control.Controls.Count > 0)
                {
                    UpdateControlCollection(control.Controls);
                }
            }
        }

        public object Clone()
        {
            MetroStyleManager metroStyleManager = new MetroStyleManager();
            metroStyleManager.metroTheme = Theme;
            metroStyleManager.metroStyle = Style;
            metroStyleManager.ownerForm = null;
            return metroStyleManager;
        }
    
    }

}
