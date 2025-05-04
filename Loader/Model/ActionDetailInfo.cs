using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LoALoader.Model
{
    public class ActionDetailInfo
    {
        [XmlElement("Direction")]
        public string Direction { get; set; }

        [XmlElement("Pivot")]
        public Pivot Pivot { get; set; }

        [XmlElement("Head")]
        public Head Head { get; set; }
    }

    public class Pivot
    {
        [XmlAttribute("pivot_x")]
        public float PivotX { get; set; }

        [XmlAttribute("pivot_y")]
        public float PivotY { get; set; }
    }

    public class Head
    {
        [XmlAttribute("head_x")]
        public float HeadX { get; set; }

        [XmlAttribute("head_y")]
        public float HeadY { get; set; }

        [XmlAttribute("rotation")]
        public float Rotation { get; set; }

        [XmlAttribute("head_enable")]
        public string FakeHeadEnable { get; set; }

        [XmlIgnore]
        public bool HeadEnable
        {
            get
            {
                switch (FakeHeadEnable)
                {
                    case "True":
                    case "true":
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
