namespace FTPFiles
{
    public partial class FTPFiles
    {
        /// <summary>
        /// struktura FTPVoid reprezentuje výsledek operace pro získání informací o souborech a složkách z FTP serveru.
        /// </summary>
        public struct FTPVoid
        {
            /// <summary>
            /// Pole FTP objektů reprezentujících soubory a složky na FTP serveru.
            /// </summary>
            public FTP[] FTP { get; set; }

            /// <summary>
            /// Textová zpráva s popisem výjimky, pokud operace selže.
            /// </summary>
            public string Exception { get; set; }
        }

    }
}
