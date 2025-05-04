using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LoALoader.Model
{
    [XmlRoot("ModInfo")]
    public class ModInfo
    {
        [XmlElement("ClothInfo")]
        public ClothInfo ClothInfo { get; set; }
    }

    public class ClothInfo
    {
        [XmlElement("Name")]
        public string Name { get; set; }

        [XmlElement("Prefab")]
        public string Prefab { get; set; }

        [XmlElement("Default")]
        public ActionDetailInfo Default { get; set; }

        [XmlElement("Guard")]
        public ActionDetailInfo Guard { get; set; }

        [XmlElement("Evade")]
        public ActionDetailInfo Evade { get; set; }

        [XmlElement("Damaged")]
        public ActionDetailInfo Damaged { get; set; }

        [XmlElement("Slash")]
        public ActionDetailInfo Slash { get; set; }

        [XmlElement("Penetrate")]
        public ActionDetailInfo Penetrate { get; set; }

        [XmlElement("Hit")]
        public ActionDetailInfo Hit { get; set; }

        [XmlElement("Move")]
        public ActionDetailInfo Move { get; set; }

        [XmlElement("Standing")]
        public ActionDetailInfo Standing { get; set; }

        [XmlElement("NONE")]
        public ActionDetailInfo NONE { get; set; }

        [XmlElement("Fire")]
        public ActionDetailInfo Fire { get; set; }

        [XmlElement("Aim")]
        public ActionDetailInfo Aim { get; set; }

        [XmlElement("Special")]
        public ActionDetailInfo Special { get; set; }

        [XmlElement("S1")]
        public ActionDetailInfo S1 { get; set; }

        [XmlElement("S2")]
        public ActionDetailInfo S2 { get; set; }

        [XmlElement("S3")]
        public ActionDetailInfo S3 { get; set; }

        [XmlElement("S4")]
        public ActionDetailInfo S4 { get; set; }

        [XmlElement("S5")]
        public ActionDetailInfo S5 { get; set; }

        [XmlElement("Slash2")]
        public ActionDetailInfo Slash2 { get; set; }

        [XmlElement("Penetrate2")]
        public ActionDetailInfo Penetrate2 { get; set; }

        [XmlElement("Hit2")]
        public ActionDetailInfo Hit2 { get; set; }

        [XmlElement("S6")]
        public ActionDetailInfo S6 { get; set; }

        [XmlElement("S7")]
        public ActionDetailInfo S7 { get; set; }

        [XmlElement("S8")]
        public ActionDetailInfo S8 { get; set; }

        [XmlElement("S9")]
        public ActionDetailInfo S9 { get; set; }

        [XmlElement("S10")]
        public ActionDetailInfo S10 { get; set; }

        [XmlElement("S11")]
        public ActionDetailInfo S11 { get; set; }

        [XmlElement("S12")]
        public ActionDetailInfo S12 { get; set; }

        [XmlElement("S13")]
        public ActionDetailInfo S13 { get; set; }

        [XmlElement("S14")]
        public ActionDetailInfo S14 { get; set; }

        [XmlElement("S15")]
        public ActionDetailInfo S15 { get; set; }
    }
}
