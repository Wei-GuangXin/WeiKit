using System;
using System.Collections.Generic;

namespace WeiKit
{
    public class States
    {
        /// <summary>
        /// 状态类型
        /// </summary>
        public class State
        {
            /// <summary>
            /// 状态的严重程度类型
            /// </summary>
            public enum StateType
            {
                /// <summary>
                /// 正常
                /// </summary>
                Normal,
                /// <summary>
                /// 消息
                /// </summary>
                Message,
                /// <summary>
                /// 警告
                /// </summary>
                Warning,
                /// <summary>
                /// 错误
                /// </summary>
                Error
            }
            /// <summary>
            /// 状态的严重程度
            /// </summary>
            public StateType Type = StateType.Normal;
            /// <summary>
            /// 状态标题
            /// </summary>
            public string Name = string.Empty;
            /// <summary>
            /// 状态详细解释文本
            /// </summary>
            public string Text = string.Empty;
            /// <summary>
            /// 触发该状态的原因
            /// </summary>
            public string Reason = string.Empty;
            /// <summary>
            /// 表示该状态是否触发？
            /// </summary>
            public bool IsCurrently = false;
        }
        /// <summary>
        /// 状态列表
        /// </summary>
        public List<State> states = new List<State>();
        /// <summary>
        /// 状态触发改变事件
        /// </summary>
        public event EventHandler<State> StateCurrentlyChange;
        /// <summary>
        /// 触发事件的方法
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnCurrentlyChange(State e)
        {
            StateCurrentlyChange?.Invoke(this, e);
        }

        /// <summary>
        /// 指定类型的触发数量。由 <see cref="RecalculateCounts"/> 自动维护。
        /// </summary>
        public int AllCurrentlyPcs, NormalCurrentlyPcs, MessageCurrentlyPcs, WarningCurrentlyPcs, ErrorCurrentlyPcs;

        /// <summary>
        /// 重新统计各类已触发状态的数量，写入 <see cref="AllCurrentlyPcs"/> 等字段。
        /// 定义状态、改变触发值时会自动调用；若直接修改了 <c>states[i].IsCurrently</c> 请手动调用一次。
        /// </summary>
        public void RecalculateCounts()
        {
            int all = 0, normal = 0, message = 0, warning = 0, error = 0;
            foreach (var s in states)
            {
                if (s == null || !s.IsCurrently) continue;
                all++;
                switch (s.Type)
                {
                    case State.StateType.Normal: normal++; break;
                    case State.StateType.Message: message++; break;
                    case State.StateType.Warning: warning++; break;
                    case State.StateType.Error: error++; break;
                }
            }
            AllCurrentlyPcs = all;
            NormalCurrentlyPcs = normal;
            MessageCurrentlyPcs = message;
            WarningCurrentlyPcs = warning;
            ErrorCurrentlyPcs = error;
        }
        /// <summary>
        /// 获取所有已触发的状态列表
        /// </summary>
        public List<State> GetAllCurrentlys()
        {
            List<State> ss = new List<State>();
            foreach (var s in states)
                if (s.IsCurrently) ss.Add(s);
            return ss;
        }
        /// <summary>
        /// 获取所有已触发的正常状态列表
        /// </summary>
        /// <returns></returns>
        public List<State> GetNormalCurrentlys()
        {
            List<State> ss = new List<State>();
            foreach (var s in states)
                if (s.IsCurrently && s.Type == State.StateType.Normal) ss.Add(s);
            return ss;
        }
        /// <summary>
        /// 获取所有已触发的消息状态列表
        /// </summary>
        /// <returns></returns>
        public List<State> GetMessageCurrentlys()
        {
            List<State> ss = new List<State>();
            foreach (var s in states)
                if (s.IsCurrently && s.Type == State.StateType.Message) ss.Add(s);
            return ss;
        }
        /// <summary>
        /// 获取所有已触发的警告状态列表
        /// </summary>
        /// <returns></returns>
        public List<State> GetWarningCurrentlys()
        {
            List<State> ss = new List<State>();
            foreach (var s in states)
                if (s.IsCurrently && s.Type == State.StateType.Warning) ss.Add(s);
            return ss;
        }
        /// <summary>
        /// 获取所有已触发的错误状态列表
        /// </summary>
        /// <returns></returns>
        public List<State> GetErrorCurrentlys()
        {
            List<State> ss = new List<State>();
            foreach (var s in states)
                if (s.IsCurrently && s.Type == State.StateType.Error) ss.Add(s);
            return ss;
        }
        /// <summary>
        /// 定义一个状态
        /// </summary>
        /// <param name="name">状态标题</param>
        /// <param name="type">状态类型</param>
        /// <param name="text">状态详细解释文本</param>
        /// <param name="reason">状态触发原因</param>
        /// <returns>操作是否成功</returns>
        public bool DefinitionState(string name, State.StateType type, string text = "", string reason = "")
        {
            foreach (var s in states)
                if (s.Name == name)
                {
                    Console.WriteLine($"已存在同名状态，定义<{name}>状态失败。");
                    return false;
                }
            states.Add(new State
            {
                Name = name,
                Type = type,
                Text = text,
                Reason = reason
            });
            RecalculateCounts();
            Console.WriteLine($"定义<{name}>状态完成。");
            return true;
        }
        /// <summary>
        /// 触发/取消触发一个状态
        /// </summary>
        /// <param name="name">状态标题</param>
        /// <param name="currently">是否触发</param>
        /// <returns>操作是否成功</returns>
        public bool SetStateCurrently(string name, bool currently)
        {
            foreach (var s in states)
                if (s.Name == name)
                {
                    s.IsCurrently = currently;
                    RecalculateCounts();
                    OnCurrentlyChange(s);
                    Console.WriteLine($"<{name}>状态触发已变更。");
                    return true;
                }
            Console.WriteLine($"更改<{name}>状态的触发值失败，未找到该状态。");
            return false;
        }
    }
}
