// --------------------------------------------------------------------------------------
// <copyright file="program.cs" company="André Krämer - Software, Training & Consulting">
//      Copyright (c) 2015 André Krämer http://andrekraemer.de
// </copyright>
// <summary>
//  Micro O/R Mapper Demo Project
// </summary>
// --------------------------------------------------------------------------------------
using System;

namespace MicroOrmSample
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Dapper Samples");
            ISample sample = new DapperSample();
            SampleRunner(sample);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();

            Console.WriteLine("PetaPoco Samples");
            sample = new PetaPocoSample();
            SampleRunner(sample);
            Console.ReadLine();

        }

        private static void SampleRunner(ISample sample)
        {
                        sample.SimpleQuery();
            sample.ParamQuery();
            sample.ManyToOneRelations();
            sample.Relations();
            sample.DynamicQuery();
            sample.SP();
            sample.Insert();
            sample.Update();
            sample.Delete();
        }
    }
}
