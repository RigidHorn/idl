using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace compiler
{
  public class FuncModel
  {
    [XmlAttribute("name")]
    public string name { get; set; }

    [XmlAttribute("flow")]
    public string flow { get; set; }

    [XmlElement("arg")]
    public List<ArgModel> args { get; set; }
  }
}
