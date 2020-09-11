using System;
using System.Collections.Generic;

namespace Sssl.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var ssslObj = SsslObject.ConvertFrom(new
            {
                Text = "ab\n\rc",
                Int = 12,
                Double = 12.6,
                Bool = false,
                Tuple = (12, "abc"),
                Obj = new
                {
                    A = true,
                    B = false,
                },
                Array = new[]
                {
                    1,
                    2,
                    3,
                    4,
                },
                Null = (object)null,
                Dict = new Dictionary<int, int>
                {
                    [1] = 5,
                    [2] = 6,
                    [12] = 9,
                },
            });
            
            var sssl = ssslObj.ToDynamic();

            if (sssl.Has("Text"))
            {

            }

            Console.WriteLine(sssl);
            Console.WriteLine(sssl.Text());
            Console.WriteLine(sssl.Has("Text"));
            Console.WriteLine(sssl["Text"]);
            Console.WriteLine((string)sssl.Text);
            sssl.Text = SsslObject.Undefined;
            sssl.Value = 12.5;
            sssl["Aa"] = new { B = 12, C = 15.0 };
            sssl.Array.Add(15);
            Console.WriteLine(sssl);
            Console.WriteLine((bool)sssl.Bool);
            Console.WriteLine((bool?)sssl.Obj.A);
            Console.WriteLine((bool?)sssl.Null);
            Console.WriteLine(sssl.Array);
            Console.WriteLine((int[])sssl.Array);
            Console.WriteLine((double)sssl.Array[1]);
            Console.WriteLine((SsslObject)sssl.Array);
            Console.WriteLine((Dictionary<string, dynamic>)sssl);

            var data = (TestData)SsslObject.ConvertFrom(new { Text = "abc", Num = 12 }).ToDynamic();
            Console.WriteLine(data.Text);
            Console.WriteLine(data.Num);

            var data2 = new Base[]
            {
                new Sub1 { Text = "1234" },
                new Sub2 { Abc = 12 },
            };
            var serializedData2 = SsslObject.ConvertFrom(data2);
            Console.WriteLine(serializedData2);
            Console.WriteLine("--[");
            var data2_2 = serializedData2.ConvertTo<Base[]>();
            foreach (var d in data2_2)
                Console.WriteLine(d);
            Console.WriteLine("--]");

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");
                var src = Console.ReadLine();
                try
                {
                    Console.WriteLine(SsslObject.Parse(src));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(ex.Message);
                    Console.ResetColor();
                }
            }
        }
    }

    public class TestData
    {
        public string Text { get; set; }

        public int Num { get; set; }
    }

    public abstract class Base
    {

    }

    public class Sub1 : Base
    {
        public string Text { get; set; }
    }

    public class Sub2 : Base
    {
        public int Abc { get; set; }
    }
}
