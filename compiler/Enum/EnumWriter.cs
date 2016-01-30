using System;
using System.IO;

namespace compiler
{
  class EnumWriter
  {
    EnumModel model_;
    StreamWriter cp_writer_;
    StreamWriter cs_writer_;

    public EnumWriter(EnumModel model)
    {
      model_ = model;
    }

    public void Exec()
    {
      string cp_name = "";
      foreach (var c in model_.name)
      {
        if (Char.IsLower(c))
        {
          cp_name += c;
          continue;
        }

        if (cp_name.Length != 0)
          cp_name += '_';
        cp_name += Char.ToLower(c);
      }

      cp_writer_ = new StreamWriter(cp_name + ".h");
      cs_writer_ = new StreamWriter(model_.name + ".cs");

      cp_writer_.WriteLine("#pragma once");
      cp_writer_.WriteLine("enum class " + model_.name + '{');
      cs_writer_.WriteLine("enum " + model_.name);
      cs_writer_.WriteLine('{');

      foreach (var enumerator in model_.enumerators)
      {
        cp_writer_.WriteLine('k' + enumerator.name + ',');
        cs_writer_.WriteLine('k' + enumerator.name + ',');
      }

      cp_writer_.WriteLine("};");
      cs_writer_.WriteLine('}');

      // バッファフラッシュ.
      cp_writer_.Flush();
      cs_writer_.Flush();
    }
  }
}
