using System.Xml;

namespace EasyMorph.TestTask.Common
{
    public static class XmlNameCompareHelper
    {
        /// <summary>
        /// Attempts to effectiv retrieve the value of the first XML attribute that matches the specified predicate.
        /// </summary>
        public static bool TryGetAttr(this XmlReader reader, Func<XmlReader, bool> isAttr, out string value)
        {
            if (reader.HasAttributes)
            {
                while (reader.MoveToNextAttribute())
                {
                    if (isAttr(reader))
                    {
                        if (!string.IsNullOrEmpty(reader.Value))
                        {
                            value = reader.Value;
                            reader.MoveToElement();
                            return true;
                        }
                        break;
                    }
                }
                reader.MoveToElement();
            }
            value = null;
            return false;
        }

        /// <summary>
        /// Attempts to read the text content of the current XML element.
        /// </summary>
        public static bool TryGetElementContent(this XmlReader reader, out string value)
        {
            if (reader.Read() && reader.NodeType == XmlNodeType.Text && !string.IsNullOrEmpty(reader.Value))
            {
                value = reader.Value;
                return true;
            }
            value = null;
            return false;
        }
    }
}