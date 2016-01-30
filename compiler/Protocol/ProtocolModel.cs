using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace compiler
{
  [XmlRoot("protocol")]
  public class ProtocolModel
  {
    [XmlAttribute("srv_name")]
    public string srv_name { get; set; }

    [XmlAttribute("cli_name")]
    public string cli_name { get; set; }

    [XmlAttribute("buf_size")]
    public int buf_size { get; set; }

    [XmlElement("func")]
    public List<FuncModel> funcs { get; set; }
  }
}
