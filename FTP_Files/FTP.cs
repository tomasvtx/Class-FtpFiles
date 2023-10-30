using System;

namespace FTPFiles
{
    public partial class FTPFiles
    {
        /// <summary>
        /// Struktura FTP reprezentuje informace o souborech a složkách na FTP serveru.
        /// </summary>
        public struct FTP
        {
            /// <summary>
            /// Datum vytvoření souboru nebo složky.
            /// </summary>
            public DateTime? Date { get; set; }

            /// <summary>
            /// Název souboru nebo složky.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Velikost souboru v bajtech, případně "0" pro složky.
            /// </summary>
            public string Size { get; set; }

            /// <summary>
            /// Určuje, zda se jedná o složku (true) nebo soubor (false).
            /// </summary>
            public bool IsFolder { get; set; }
        }
    }
}
