// --------------------------------------------------------------------------------------
// <copyright file="ISample.cs" company="André Krämer - Software, Training & Consulting">
//      Copyright (c) 2015 André Krämer http://andrekraemer.de
// </copyright>
// <summary>
//  Micro O/R Mapper Demo Project
// </summary>
// --------------------------------------------------------------------------------------
using System.Data.SqlClient;

namespace MicroOrmSample
{
    public interface ISample
    {
        void SimpleQuery();
        void ParamQuery();
        void ManyToOneRelations();
        void Relations();
        void DynamicQuery();
        void SP();
        void Insert();
        void Update();
        void Delete();
        SqlConnection GetConnection();
    }
}