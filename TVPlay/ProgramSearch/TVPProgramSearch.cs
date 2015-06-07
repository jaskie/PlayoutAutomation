using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using MySql.Data.MySqlClient;

namespace TAS.Client.ProgramSearch
{
    class TVPProgramSearch : IProgramSearch
    {
        MySqlConnection connection;
        public TVPProgramSearch()
        {
            Initialize();
            connection.Open();
        }
        
        ~TVPProgramSearch()
        {
            connection.Close();
        }

        private void Initialize()
        {
            string server = "localhost";
            string database = "ADP";
            string uid = "root";
            string password = "haslo";
            string connectionString;
            connectionString = "SERVER=" + server + ";" + "DATABASE=" +
            database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
        }

        public ObservableCollection<Program> Search(string Title)
        {
            ObservableCollection<Program> Result = new ObservableCollection<Program>();
            MySqlCommand cmd = new MySqlCommand("SELECT idAudycja, Identyfikator, Nazwa, CzasTrwania FROM adp.audycja where Nazwa like @SearchExpr", connection);
            cmd.Parameters.AddWithValue("@SearchExpr", Title+'%');
            MySqlDataReader dataReader = cmd.ExecuteReader();
            try
            {
                while (dataReader.Read())
                {
                    Result.Add(new Program
                    {
                        idProgram = dataReader.GetUInt64("idAudycja"),
                        Identifier = dataReader.GetString("Identyfikator"),
                        Title = dataReader.GetString("Nazwa"),
                        Duration = dataReader.GetTimeSpan("CzasTrwania")
                    });
                }
            }
            finally
            {
                dataReader.Close();
            }
            return Result;
        }
    }
}
