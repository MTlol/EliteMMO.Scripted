﻿using System;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using System.Xml;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace FormSerialisation
{
    public static class FormSerialisor
    {
        /*
         * Drop this class into your project, and add the following line at the top of any class/form that wishes to use it...
           using FormSerialisation;
           To use the code, simply call FormSerialisor.Serialise(FormOrControlToBeSerialised, FullPathToXMLFile)
         * 
         * For more details, see http://www.codeproject.com/KB/dialog/SavingTheStateOfAForm.aspx
         * 
         * Last updated 13th June '10 to account for the odd behaviour of the two Panel controls in a SplitContainer (see the article)
         */
        public static void Serialise(Control c, string XmlFileName)
        {
            XmlTextWriter xmlSerialisedForm = new XmlTextWriter(XmlFileName, System.Text.Encoding.Default);
            xmlSerialisedForm.Formatting = Formatting.Indented;
            xmlSerialisedForm.WriteStartDocument();
            xmlSerialisedForm.WriteStartElement("ChildForm");
            // enumerate all controls on the form, and serialise them as appropriate
            AddChildControls(xmlSerialisedForm, c);
            xmlSerialisedForm.WriteEndElement(); // ChildForm
            xmlSerialisedForm.WriteEndDocument();
            xmlSerialisedForm.Flush();
            xmlSerialisedForm.Close();
        }
        private static void AddChildControls(XmlTextWriter xmlSerialisedForm, Control c)
        {
            foreach (Control childCtrl in c.Controls)
            {
                if (!(childCtrl is Label) && !(childCtrl is ListView) && !(childCtrl is MenuStrip) &&
                      childCtrl.GetType().ToString() != "System.Windows.Forms.UpDownBase+UpDownEdit" &&
                      childCtrl.GetType().ToString() != "System.Windows.Forms.UpDownBase+UpDownButtons" &&
                      childCtrl.GetType().ToString() != "System.Windows.Forms.Button")
                {
                    // serialise this control
                    xmlSerialisedForm.WriteStartElement("Control");
                    xmlSerialisedForm.WriteAttributeString("Type", childCtrl.GetType().ToString());
                    xmlSerialisedForm.WriteAttributeString("Name", childCtrl.Name);
                    if (childCtrl is TextBox)
                    {
                        xmlSerialisedForm.WriteElementString("Text", ((TextBox)childCtrl).Text);
                    }
                    else if (childCtrl is RadioButton)
                    {
                        xmlSerialisedForm.WriteElementString("Checked", ((RadioButton)childCtrl).Checked.ToString());
                    }
                    else if (childCtrl is NumericUpDown)
                    {
                        xmlSerialisedForm.WriteElementString("Value", ((NumericUpDown)childCtrl).Value.ToString());
                        xmlSerialisedForm.WriteElementString("Enabled", ((NumericUpDown)childCtrl).Enabled.ToString());
                    }
                    else if (childCtrl is GroupBox)
                    {
                        xmlSerialisedForm.WriteElementString("Enabled", ((GroupBox)childCtrl).Enabled.ToString());
                    }
                    else if (childCtrl is CheckedListBox)
                    {
                        // need to account for multiply selected items
                        CheckedListBox lst = (CheckedListBox)childCtrl;
                        /*for (int i = 0; i < lst.Items.Count; i++)
                        {
                            xmlSerialisedForm.WriteElementString("list"+i.ToString(), (lst.Items[i].ToString()));
                        }
                        xmlSerialisedForm.WriteElementString("listcount", (lst.Items.Count.ToString())); */
                        for (int i = 0; i < lst.CheckedIndices.Count; i++)
                        {
                            xmlSerialisedForm.WriteElementString("SelectedIndex" + i.ToString(), (lst.CheckedIndices[i].ToString()));
                        }
                        xmlSerialisedForm.WriteElementString("SelectedIndexcount", (lst.CheckedIndices.Count.ToString()));
                    }
                    else if (childCtrl is ComboBox)
                    {
                        xmlSerialisedForm.WriteElementString("Text", ((ComboBox)childCtrl).Text);
                        //xmlSerialisedForm.WriteElementString("SelectedIndex", ((ComboBox)childCtrl).SelectedIndex.ToString());
                    }
                    /*else if (childCtrl is ListBox)
                    {
                        // need to account for multiply selected items
                        ListBox lst = (ListBox)childCtrl;
                        if (lst.SelectedIndex == -1)
                        {
                            xmlSerialisedForm.WriteElementString("SelectedIndex", "-1");
                        }
                        else
                        {
                            for (int i = 0; i < lst.SelectedIndices.Count; i++)
                            {
                                xmlSerialisedForm.WriteElementString("SelectedIndex", (lst.SelectedIndices[i].ToString()));
                            }
                        }
                    }*/
                    else if (childCtrl is CheckBox)
                    {
                        xmlSerialisedForm.WriteElementString("Checked", ((CheckBox)childCtrl).Checked.ToString());
                        xmlSerialisedForm.WriteElementString("Enabled", ((CheckBox)childCtrl).Enabled.ToString());
                    }
                    // this next line was taken from http://stackoverflow.com/questions/391888/how-to-get-the-real-value-of-the-visible-property
                    // which dicusses the problem of child controls claiming to have Visible=false even when they haven't, based on the parent
                    // having Visible=true
                    //bool visible = (bool)typeof(Control).GetMethod("GetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(childCtrl, new object[] { 2 });
                    //xmlSerialisedForm.WriteElementString("Visible", visible.ToString());
                    // see if this control has any children, and if so, serialise them
                    if (childCtrl.HasChildren)
                    {
                        if (childCtrl is SplitContainer)
                        {
                            // handle this one as a special case
                            AddChildControls(xmlSerialisedForm, ((SplitContainer)childCtrl).Panel1);
                            AddChildControls(xmlSerialisedForm, ((SplitContainer)childCtrl).Panel2);
                        }
                        else
                        {
                            AddChildControls(xmlSerialisedForm, childCtrl);
                        }
                    }
                    xmlSerialisedForm.WriteEndElement(); // Control
                }
            }
        }
        public static void Deserialise(Control c, string XmlFileName)
        {
            if (File.Exists(XmlFileName))
            {
                XmlDocument xmlSerialisedForm = new XmlDocument();
                xmlSerialisedForm.Load(XmlFileName);
                XmlNode topLevel = xmlSerialisedForm.ChildNodes[1];
                foreach (XmlNode n in topLevel.ChildNodes)
                {
                    SetControlProperties((Control)c, n);
                }
            }
        }
        private static void SetControlProperties(Control currentCtrl, XmlNode n)
        {
            // get the control's name and type
            string controlName = n.Attributes["Name"].Value;
            if (controlName == "") return;
            string controlType = n.Attributes["Type"].Value;
            // find the control
            Control[] ctrl = currentCtrl.Controls.Find(controlName, true);
            if (ctrl.Length == 0)
            {
                // can't find the control
            }
            else
            {
                Control ctrlToSet = GetImmediateChildControl(ctrl, currentCtrl);
                if (ctrlToSet != null)
                {
                    if (ctrlToSet.GetType().ToString() == controlType)
                    {
                        // the right type too ;-)
                        switch (controlType)
                        {
                            case "System.Windows.Forms.CheckedListBox":
                                // need to account for multiply selected items
                                CheckedListBox ltr = (CheckedListBox)ctrlToSet;
                                //var Lcount=Convert.ToInt32(n["listcount"].InnerText);
                                var Icount=Convert.ToInt32(n["SelectedIndexcount"].InnerText);
                                /* ltr.Items.Clear();
                                for (int i = 0; i < Lcount; i++)
                                {
                                    if (!ltr.Items.Contains(n["list" + i.ToString()].InnerText))
                                        ltr.Items.Add(n["list"+i.ToString()].InnerText);
                                } */
                                ltr.SelectedIndices.Clear();
                                for (int i = 0; i < Icount; i++)
                                {
                                    ltr.SetItemChecked(Convert.ToInt16(n["SelectedIndex" + i.ToString()].InnerText), true);
                                }
                                break;
                            case "System.Windows.Forms.RadioButton":
                                ((RadioButton)ctrlToSet).Checked = Convert.ToBoolean(n["Checked"].InnerText);
                                break;
                            case "System.Windows.Forms.GroupBox":
                                ((GroupBox)ctrlToSet).Enabled = Convert.ToBoolean(n["Enabled"].InnerText);
                                break;
                            case "System.Windows.Forms.NumericUpDown":
                                ((NumericUpDown)ctrlToSet).Value = Convert.ToDecimal(n["Value"].InnerText);
                                ((NumericUpDown)ctrlToSet).Enabled = Convert.ToBoolean(n["Enabled"].InnerText);
                                break;
                            case "System.Windows.Forms.TextBox":
                                ((TextBox)ctrlToSet).Text = n["Text"].InnerText;
                                break;
                            case "System.Windows.Forms.ComboBox":
                                ((ComboBox)ctrlToSet).SelectedText = n["Text"].InnerText;
                                //((System.Windows.Forms.ComboBox)ctrlToSet).SelectedIndex = Convert.ToInt32(n["SelectedIndex"].InnerText);
                                break;
                            /* case "System.Windows.Forms.ListBox":
                                // need to account for multiply selected items
                                ListBox lst = (ListBox)ctrlToSet;
                                XmlNodeList xnlSelectedIndex = n.SelectNodes("SelectedIndex");
                                for (int i = 0; i < xnlSelectedIndex.Count; i++)
                                {
                                    lst.SelectedIndex = Convert.ToInt32(xnlSelectedIndex[i].InnerText);
                                }
                                break; */
                            case "System.Windows.Forms.CheckBox":
                                ((CheckBox)ctrlToSet).Checked = Convert.ToBoolean(n["Checked"].InnerText);
                                break;
                        }
                        //ctrlToSet.Visible = Convert.ToBoolean(n["Visible"].InnerText);
                        // if n has any children that are controls, deserialise them as well
                        if (n.HasChildNodes && ctrlToSet.HasChildren)
                        {
                            XmlNodeList xnlControls = n.SelectNodes("Control");
                            foreach (XmlNode n2 in xnlControls)
                            {
                                SetControlProperties(ctrlToSet, n2);
                            }
                        }
                    }
                    else
                    {
                        // not the right type
                    }
                }
                else
                {
                    // can't find a control whose parent is the current control
                }
            }
        }
        private static Control GetImmediateChildControl(Control[] ctrl, Control currentCtrl)
        {
            Control c = null;
            for (int i = 0; i < ctrl.Length; i++)
            {
                if ((ctrl[i].Parent.Name == currentCtrl.Name) || (currentCtrl is SplitContainer && ctrl[i].Parent.Parent.Name == currentCtrl.Name))
                {
                    c = ctrl[i];
                    break;
                }
            }
            return c;
        }
    }
}
