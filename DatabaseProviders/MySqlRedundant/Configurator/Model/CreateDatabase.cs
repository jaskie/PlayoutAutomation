using System.Linq;
using TAS.Database.MySqlRedundant;

namespace TAS.Database.MySqlRedundant.Configurator.Model
{
    public class CreateDatabase
    {
        private readonly DatabaseMySqlRedundant _db;

        public CreateDatabase(DatabaseMySqlRedundant db)
        {
            _db = db;
            Collation = Collations.FirstOrDefault();
        }
        public string ConnectionString { get; set; }
        public string Collation { get; set; }
        public static readonly string[] Collations = {"utf8_general_mysql500_ci",
                                                "utf8_vietnamese_ci",
                                                "utf8_unicode_520_ci",
                                                "utf8_croatian_ci",
                                                "utf8_german2_ci",
                                                "utf8_sinhala_ci",
                                                "utf8_hungarian_ci", 
                                                "utf8_esperanto_ci", 
                                                "utf8_persian_ci",
                                                "utf8_roman_ci", 
                                                "utf8_spanish2_ci",
                                                "utf8_slovak_ci", 
                                                "utf8_lithuanian_ci",
                                                "utf8_danish_ci", 
                                                "utf8_czech_ci",
                                                "utf8_turkish_ci",
                                                "utf8_swedish_ci",
                                                "utf8_spanish_ci",
                                                "utf8_estonian_ci",
                                                "utf8_polish_ci",
                                                "utf8_slovenian_ci", 
                                                "utf8_romanian_ci",
                                                "utf8_latvian_ci",
                                                "utf8_icelandic_ci", 
                                                "utf8_unicode_ci",
                                                "utf8_bin", 
                                                "utf8",
                                                "utf8_general_ci",
                                                 };

        public bool CreateEmptyDatabase()
        {
            return _db.CreateEmptyDatabase(ConnectionString, Collation);
        }
    }
}
