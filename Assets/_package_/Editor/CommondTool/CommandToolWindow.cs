using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommandTool
{
    /// <summary>
    /// 一个简单的命令行工具
    /// </summary>
    public class CommandToolWindow : EditorWindow
    {
        [MenuItem("Tool/Command Tool")]
        public static void ShowCommandToolWindow()
        {
            var ct = GetWindow<CommandToolWindow>();
            ct.titleContent = new GUIContent("Command Tool");
        }

        /// <summary>
        /// 进程代理类
        /// </summary>
        private ProcessProxy _proxy;

        /// <summary>
        /// 命令执行完成的返回消息队列
        /// </summary>
        private Queue<string> _returnMsgs;

        private bool _cmdReturnFlag = false;

        private void OnEnable()
        {
            InitUI();
        }

        private void StartCMD()
        {
            _returnMsgs = new Queue<string>();
            _proxy = new ProcessProxy();
            _proxy.Start();
            Debug.Log("启动cmd");
        }


        private void InitUI()
        {
            var start = new Button
            {
                name = "btn_start_cmd",
                text = "启动cmd"
            };
            start.clicked += () =>
            {
                StartCMD();
                StartButton.SetEnabled(false);
                CloseButton.SetEnabled(true);
                OutputBox.SetEnabled(true);
                SetInputEnable(true);
            };
            rootVisualElement.Add(start);

            // 关闭cmd进程
            var close = new Button
            {
                name = "btn_close_cmd",
                text = "关闭cmd"
            };
            close.clicked += () =>
            {
                CloseProxy();
                StartButton.SetEnabled(true);
                CloseButton.SetEnabled(false);
                OutputBox.SetEnabled(false);
                SetInputEnable(false);
            };
            close.SetEnabled(false);
            rootVisualElement.Add(close);

            var output = new Box
            {
                name = "box_output"
            };
            {
                // 滚动视图
                var scrollView = new ScrollView
                {
                    name = "sv_output"
                };
                {
                    // 结果输出
                    var label = new Label
                    {
                        name = "lab_output", text = ""
                    };
                    scrollView.Add(label);
                }
                output.Add(scrollView);
            }
            output.SetEnabled(false);
            rootVisualElement.Add(output);

            // cmd命令输入框
            var inputText = new TextField
            {
                name = "ipt_text"
            };
            inputText.SetEnabled(false);
            rootVisualElement.Add(inputText);

            // 确定输入命令
            var input = new Button
            {
                name = "btn_input",
                text = "确定"
            };
            input.clicked += () =>
            {
                _returnMsgs.Clear();

                RunCmd(inputText.value, (ctx) =>
                {
                    _returnMsgs = ctx.Messages;
                    _cmdReturnFlag = true;
                });
                SetInputEnable(false);
            };
            input.SetEnabled(false);
            rootVisualElement.Add(input);
        }

        private void SetInputEnable(bool enable)
        {
            InputTextField.SetEnabled(enable);
            InputButton.SetEnabled(enable);
        }

        public void RunCmd(string cmd, CommandCallback callback)
        {
            _proxy.Run(cmd, callback);
        }

        private Box OutputBox => rootVisualElement.Q<Box>("box_output");

        private ScrollView OutputScrollView => OutputBox.Q<ScrollView>("sv_output");

        private Label OutputLabel => OutputScrollView.Q<Label>("lab_output");

        private Button StartButton => rootVisualElement.Q<Button>("btn_start_cmd");

        private Button CloseButton => rootVisualElement.Q<Button>("btn_close_cmd");


        private TextField InputTextField => rootVisualElement.Q<TextField>("ipt_text");

        private Button InputButton => rootVisualElement.Q<Button>("btn_input");

        private float HighValue => OutputScrollView.verticalScroller.slider.highValue;

        private float SliderValue
        {
            get => OutputScrollView.verticalScroller.value;
            set => OutputScrollView.verticalScroller.value = value;
        }

        private void Update()
        {
            if (!_cmdReturnFlag)
            {
                return;
            }

            var command = _returnMsgs.Dequeue();
            var content = "";
            while (_returnMsgs.Count > 0)
            {
                var line = _returnMsgs.Dequeue();
                if (!line.Contains(ProcessProxy.COMMAND_RETURN))
                {
                    content += line + "\n";
                }
            }

            OutputLabel.text += $"输入命令:[{command}]\n\n";
            OutputLabel.text += $"输出:\n{content}\n";
            OutputLabel.text += $"<-------------------------------------------------->\n";
            _cmdReturnFlag = false;
            SetInputEnable(true);
        }

        private void OnDisable()
        {
            CloseProxy();
        }

        private void OnDestroy()
        {
            CloseProxy();
        }

        private void CloseProxy()
        {
            if (_proxy == null)
            {
                return;
            }

            _proxy.Close();
            _proxy = null;
            OutputLabel.text = "";
            Debug.Log("Process Proxy Close");
        }
    }
}