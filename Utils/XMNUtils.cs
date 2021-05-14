using System;
using System.Collections.Generic;
using System.Text;

namespace XMNUtils
{
    using UnityEngine;
    using System.IO;

    public static class GameObjectDumper
    {
        static private StringWriter _stringWriter;

        static private StringWriter StringWriter {
            get {
                StringWriter result;
                if ((result = _stringWriter) == null)
                {
                    result = (_stringWriter = new StringWriter());
                }
                return result;
            }
        }

        public static string DumpGameObject(GameObject gameObject)
        {
            var stringWriter = StringWriter;
            stringWriter.GetStringBuilder().Clear();
            DumpGameObjectInternal(gameObject, stringWriter);
            return stringWriter.ToString();
        }

        public static void DumpGameObject(GameObject gameObject, TextWriter writer)
        {
            DumpGameObjectInternal(gameObject, writer);
        }

        private static void DumpGameObjectInternal(GameObject gameObject, TextWriter writer, string indent = "  ")
        {
            writer.WriteLine("{0}+{1} ({2})", indent, gameObject.name, gameObject.transform.GetType().Name);

            foreach (Component component in gameObject.GetComponents<Component>())
            {
                DumpComponent(component, writer, indent + "  ");
            }

            foreach (Transform child in gameObject.transform)
            {
                DumpGameObjectInternal(child.gameObject, writer, indent + "  ");
            }
        }

        private static void DumpComponent(Component component, TextWriter writer, string indent)
        {
            writer.WriteLine("{0}{1}", indent, (component == null ? "(null)" : component.GetType().Name));
        }
    }
}
