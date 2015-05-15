using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace BabelDeobfuscator
{
    public partial class Form1 : Form
    {
        #region Declarations

        public string DirectoryName = "";
        public int ConstantKey;
        public int ConstantNum;
        public MethodDef Methoddecryption;
        public TypeDef Typedecryption;
        public MethodDef MethodeResource;
        public TypeDef TypeResource;
        public ModuleDefMD module;
        public int x;
        public int DeobedStringNumber;
        public Mode value;
        
        #endregion

        #region Designer

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void TextBox1DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void TextBox1DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array array = (Array)e.Data.GetData(DataFormats.FileDrop);
                if (array != null)
                {
                    string text = array.GetValue(0).ToString();
                    int num = text.LastIndexOf(".", StringComparison.Ordinal);
                    if (num != -1)
                    {
                        string text2 = text.Substring(num);
                        text2 = text2.ToLower();
                        if (text2 == ".exe" || text2 == ".dll")
                        {
                            Activate();
                            textBox1.Text = text;
                            int num2 = text.LastIndexOf("\\", StringComparison.Ordinal);
                            if (num2 != -1)
                            {
                                DirectoryName = text.Remove(num2, text.Length - num2);
                            }
                            if (DirectoryName.Length == 2)
                            {
                                DirectoryName += "\\";
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            module = ModuleDefMD.Load(textBox1.Text);
            FindStringDecrypterMethods(module);
            DeobedStringNumber = 0;
            DecryptStringsInMethod(module, Methoddecryption);
            if (checkBox1.Checked)
            {
                RemoveDemo(module);
            }
            string text2 = Path.GetDirectoryName(textBox1.Text);
            if (!text2.EndsWith("\\"))
            {
                text2 += "\\";
            }
            string path = text2 + Path.GetFileNameWithoutExtension(textBox1.Text) + "_patched" +
                          Path.GetExtension(textBox1.Text);
            var opts = new ModuleWriterOptions(module);
            opts.Logger = DummyLogger.NoThrowInstance;
            module.Write(path, opts);
            label2.Text = "Successfully decrypted " + DeobedStringNumber + " strings !";
            
        }

        #endregion

        #region Method

        public enum Mode
        {
            Trial,
            Normal // soon to be added, if you have any sample to give, drop me a message ...
        };

        private void RemoveDemo(ModuleDefMD module)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.Body.HasInstructions)
                    {
                        var instrs = method.Body.Instructions;
                        if (instrs.Count > 6)
                        {
                            for (int i = 0; i < method.Body.Instructions.Count; i++)
                            {
                                if (method.Body.Instructions[i].OpCode == OpCodes.Call &&
                                    method.Body.Instructions[i + 1].OpCode == OpCodes.Ldc_I8 &&
                                    method.Body.Instructions[i + 2].OpCode == OpCodes.Newobj && 
                                    method.Body.Instructions[i + 3].OpCode == OpCodes.Call &&
                                    method.Body.Instructions[i + 4].OpCode == OpCodes.Brfalse_S &&
                                    method.Body.Instructions[i + 5].OpCode == OpCodes.Newobj &&
                                    method.Body.Instructions[i + 6].OpCode == OpCodes.Throw)
                                {
                                    for (int j = 0; j < 7  ;j++)
                                        method.Body.Instructions.RemoveAt(i);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void FindStringDecrypterMethods(ModuleDefMD module)
        {
            foreach (var type in module.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody == false)
                        continue;
                    if (method.Body.HasInstructions)
                    {
                        var instrs = method.Body.Instructions;
                        if (instrs.Count > 5)
                        {
                            for (int i = 0; i < instrs.Count - 3; i++)
                            {
                                if (instrs[i].OpCode.Code == Code.Call && instrs[1].OpCode.Code == Code.Ldc_I8 &&
                                    instrs[2].OpCode.Code == Code.Newobj && instrs[3].OpCode.Code == Code.Call &&
                                    instrs[4].OpCode.Code == Code.Brfalse_S && instrs[5].OpCode.Code == Code.Newobj &&
                                    instrs[6].OpCode.Code == Code.Throw)
                                {
                                    Methoddecryption = method;
                                    Typedecryption = type;
                                    value = Mode.Trial;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        private void DecryptStringsInMethod(ModuleDefMD module, MethodDef Methoddecryption)
        {
            foreach (TypeDef type in module.Types)
            {
                foreach (MethodDef method in type.Methods)
                {
                    if (!method.HasBody)
                        break;
                    for (int i = 0; i < method.Body.Instructions.Count; i++)
                    {
                        if (method.Body.Instructions[i].OpCode == OpCodes.Ldstr && method.Body.Instructions[i + 1].IsLdcI4() && method.Body.Instructions[i+2].OpCode == OpCodes.Call)
                        {
                            if (method.Body.Instructions[i + 1].IsLdcI4())
                                ConstantNum = (method.Body.Instructions[i + 1].GetLdcI4Value());
                            if (method.Body.Instructions[i + 2].Operand.ToString().Contains(Methoddecryption.ToString()))
                            {
                                CilBody body = method.Body;
                                var string2decrypt = method.Body.Instructions[i].Operand.ToString();
                                string decryptedstring = null;

                                Assembly assembly = Assembly.LoadFile(textBox1.Text);
                                Type typez = assembly.GetType(Typedecryption.Name);
                                if (typez != null)
                                {
                                    MethodInfo methodInfo = typez.GetMethod(Methoddecryption.Name,
                                        BindingFlags.InvokeMethod | BindingFlags.Public  | BindingFlags.Static | BindingFlags.IgnoreCase);
                                    if (methodInfo != null)
                                    {
                                        object result = null;
                                        ParameterInfo[] parameters = methodInfo.GetParameters();
                                        if (parameters.Length == 0)
                                        {

                                        }
                                        else
                                        {
                                            object[] parametersArray = new object[] { string2decrypt, ConstantNum };
                                            result = methodInfo.Invoke(methodInfo, parametersArray);
                                            decryptedstring = result.ToString();
                                            DeobedStringNumber = DeobedStringNumber + 1;
                                            body.Instructions[i].OpCode = OpCodes.Ldstr;
                                            body.Instructions[i].Operand = decryptedstring;
                                            body.Instructions.RemoveAt(i + 1);
                                            body.Instructions.RemoveAt(i + 1);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
