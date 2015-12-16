﻿using System.Xml.Serialization;

namespace Ackara.Daterpillar.Transformation
{
    public class ForeignKey
    {
        public const string TagName = "foreignKey";

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("localColumn")]
        public string LocalMember { get; set; }

        [XmlAttribute("foreignTable")]
        public string ForeignObject { get; set; }

        [XmlAttribute("foreignColumn")]
        public string ForeignMember { get; set; }

        [XmlElement("onUpdate")]
        public ForeignKeyRule OnUpdate { get; set; }

        [XmlElement("onDelete")]
        public ForeignKeyRule OnDelete { get; set; }

        [XmlElement("onMatch")]
        public ForeignKeyRule OnMatch { get; set; }
    }
}