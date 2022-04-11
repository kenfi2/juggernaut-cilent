using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace juggernaut_client
{
    public class ActionEvent
    {
        private List<Action> m_funcs = new List<Action>();
        public Action Connect(Action func)
        {
            m_funcs.Add(func);

            return () => m_funcs.Remove(func);
        }
        public void Disconnect(Action func)
        {
            m_funcs.Remove(func);
        }
        public void Execute()
        {
            foreach (var action in m_funcs)
            {
                 action();
            }
        }
    }
    public class ActionEvent<T>
    {
        private List<Action<T>> m_funcs = new List<Action<T>>();
        public Action Connect(Action<T> func)
        {
            m_funcs.Add(func);

            return () => m_funcs.Remove(func);
        }
        public void Disconnect(Action<T> func)
        {
            m_funcs.Remove(func);
        }
        public void Execute(T t)
        {
            foreach (var action in m_funcs)
            {
                action(t);
            }
        }
    }
    public class ActionEvent<T, S>
    {
        private List<Action<T, S>> m_funcs = new List<Action<T, S>>();
        public Action Connect(Action<T, S> func)
        {
            m_funcs.Add(func);

            return () => m_funcs.Remove(func);
        }
        public void Disconnect(Action<T, S> func)
        {
            m_funcs.Remove(func);
        }
        public void Execute(T t, S s)
        {
            foreach (var action in m_funcs)
            {
                action(t, s);
            }
        }
    }
}
