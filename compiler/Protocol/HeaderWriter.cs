using System;
using System.IO;
using System.Collections.Generic;

namespace compiler
{
  class HeaderWriter
  {
    string flow_;
    string file_name;
    string class_name;
    StreamWriter writer_;
    ProtocolModel model_;

    public HeaderWriter(string flow, ProtocolModel model)
    {
      flow_ = flow;

      // file_name.
      if (flow_ == "s2c")
        file_name = model.srv_name + '2' + model.cli_name;
      else
        file_name = model.cli_name + '2' + model.srv_name;

      // class_name.
      bool is_upper = true;
      foreach (var c in file_name)
      {
        if (c == '_')
        {
          is_upper = true;
          continue;
        }
        if (c == '2')
        {
          is_upper = true;
          class_name += c;
          continue;
        }
        if (is_upper)
        {
          class_name += char.ToUpper(c);
          is_upper = false;
          continue;
        }
        class_name += c;
      }

      model_ = model;
    }

    public void Exec()
    {
      // ファイル作成.
      writer_ = new StreamWriter(file_name + ".h");

      // インクルードガード.
      writer_.WriteLine("#pragma once");

      // インクルード.
      writer_.WriteLine("#include<stdint.h>");

      // クラス宣言.
      writer_.WriteLine("class " + class_name + '{');

      // RPC関数.
      writer_.WriteLine("public:");
      writer_.WriteLine("void Recv(const int8_t*buf,const int32_t len);");
      writer_.WriteLine("public:");
      writer_.WriteLine("int32_t len()const{return len_;}");
      writer_.WriteLine("const int8_t*buf()const{return buf_;}");
      writer_.WriteLine("void Clear();");

      // 送受信関数.
      if (flow_ == "s2c")
      {
        WriteSendFuncs("s2c");
        WriteRecvFuncs("c2s");
      }
      else
      {
        WriteSendFuncs("c2s");
        WriteRecvFuncs("s2c");
      }

      // メンバ.
      writer_.WriteLine("private:");
      writer_.WriteLine("int32_t len_;");
      writer_.WriteLine("int8_t buf_[" + model_.buf_size + "];");

      // classを閉じる.
      writer_.WriteLine("};");

      // バッファフラッシュ.
      writer_.Flush();
    }

    void WriteSendFuncs(string flow)
    {
      writer_.WriteLine("public:");
      foreach (var func in model_.funcs)
      {
        // flow識別.
        if (func.flow != flow) continue;

        writer_.Write("void " + func.name + '(');
        WriteArg(func);
        writer_.WriteLine(");");
      }
    }

    public void WriteArg(FuncModel func)
    {
      var args = new List<string>();
      // 引数.
      foreach (var arg in func.args)
      {
        // 文字列.
        if (arg.type == "string")
        {
          args.Add("const char*" + arg.name);
          continue;
        }
        args.Add("const " + GetType(arg) + ' ' + arg.name);
      }
      writer_.Write(String.Join(",", args));
    }

    public int GetByteNum(ArgModel arg)
    {
      if (arg.type == "string")
        return -1;
      if (arg.type.Contains("byte"))
        return 1;
      if (arg.type.Contains("short"))
        return 2;
      if (arg.type.Contains("int"))
        return 4;
      if (arg.type == "float")
        return 4;

      return 0;
    }

    public string GetType(ArgModel arg)
    {
      if (arg.type == "string")
        return "char*";

      if (arg.type == "float")
        return "float";

      string str = "";
      if (arg.type[0] == 'u')
        str += 'u';
      if (arg.type == "byte")
        str += 'u';

      return str + "int" + GetByteNum(arg) * 8 + "_t";
    }

    void WriteRecvFuncs(string flow)
    {
      writer_.WriteLine("public:");
      foreach (var func in model_.funcs)
      {
        // flow識別.
        if (func.flow != flow) continue;

        writer_.Write("static void set_" + func.name + "(void (*p)(");
        WriteArg(func);
        writer_.WriteLine(")){" + func.name + "_=p;}");
      }

      writer_.WriteLine("private:");
      foreach (var func in model_.funcs)
      {
        // flow識別.
        if (func.flow != flow) continue;

        writer_.Write("static void (*" + func.name + "_)(");
        WriteArg(func);
        writer_.WriteLine(");");
      }
    }
  }
}
