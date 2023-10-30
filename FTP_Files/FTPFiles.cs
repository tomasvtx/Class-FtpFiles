using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using static FTPFiles.FTPFiles;

namespace FTPFiles
{
    public class FTPClient
    {
        /// <summary>
        /// Adresa FTP serveru, ke kterému bude klient připojen.
        /// </summary>
        private string ftpAddress;

        /// <summary>
        /// Uživatelské jméno pro autentizaci na FTP serveru (volitelné).
        /// </summary>
        private string ftpUsername;

        /// <summary>
        /// Heslo pro autentizaci na FTP serveru (volitelné).
        /// </summary>
        private string ftpPassword;

        /// <summary>
        /// Inicializuje novou instanci třídy FTPClient s určenou adresou FTP serveru a volitelným uživatelským jménem a heslem.
        /// </summary>
        /// <param name="ftpAddress">Adresa FTP serveru.</param>
        /// <param name="ftpUsername">Uživatelské jméno pro autentizaci (volitelné).</param>
        /// <param name="ftpPassword">Heslo pro autentizaci (volitelné).</param>
        public FTPClient(string ftpAddress, string ftpUsername = null, string ftpPassword = null)
        {
            this.ftpAddress = ftpAddress;
            this.ftpUsername = ftpUsername;
            this.ftpPassword = ftpPassword;
        }


        /// <summary>
        /// Skenuje FTP server rekurzivně a získá seznam souborů a složek.
        /// </summary>
        /// <returns>Objekt FTPVoid obsahující seznam souborů a složek a případnou chybu.</returns>
        public FTPVoid GetFiles()
        {
            List<FTP> files = new List<FTP>();
            Exception exception = null;

            try
            {
                ScanFolderRecursively(ftpAddress, files, ref exception);

                return new FTPVoid { FTP = files.ToArray(), Exception = exception?.Message };
            }
            catch (Exception ex)
            {
                return new FTPVoid { FTP = null, Exception = ex?.Message };
            }
        }


        /// <summary>
        /// Rekurzivně skenuje FTP složku a přidává soubory a složky do seznamu.
        /// </summary>
        /// <param name="folder">Aktuální složka ke skenování.</param>
        /// <param name="files">Seznam obsahující naskenované soubory a složky.</param>
        /// <param name="exception">Reference na výjimku, pokud nastala chyba během skenování.</param>
        private void ScanFolderRecursively(string folder, List<FTP> files, ref Exception exception)
        {
            try
            {
                // Přidáme naskenované složky a soubory do seznamu
                files.AddRange(ScanFTP(folder));

                // Pro každou složku v seznamu
                foreach (var folderFTP in files.Where(f => f.IsFolder).ToList())
                {
                    // Znovu zavoláme ScanFolderRecursively pro tuto složku
                    ScanFolderRecursively(folderFTP.Name, files, ref exception);
                    files.Remove(folderFTP);
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        }

        /// <summary>
        /// Skenuje FTP adresu a získá seznam souborů a složek.
        /// </summary>
        /// <param name="ftpAddress">Adresa FTP serveru ke skenování.</param>
        /// <returns>Pole objektů FTP reprezentující seznam souborů a složek.</returns>
        public FTP[] ScanFTP(string ftpAddress)
        {
            List<FTP> filesFTP = new List<FTP>();
            try
            {
                // Ověříme, zda adresa FTP končí lomítkem, a případně ho přidáme
                if (!ftpAddress.EndsWith("/"))
                {
                    ftpAddress = ftpAddress + "/";
                }

                // Vytvoříme žádost na získání seznamu souborů a složek
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpAddress);
                request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

                // Přihlášení na FTP (pokud jsou poskytnuty uživatelské jméno a heslo)
                if (ftpUsername != null && ftpPassword != null && ftpUsername.Length > 0 && ftpPassword.Length > 0)
                {
                    request.Credentials = new NetworkCredential(ftpUsername, ftpPassword);
                }

                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            // Přečteme výstup serveru obsahující seznam souborů a složek
                            string responseText = reader.ReadToEnd();
                            string[] stringSplit = responseText.Split('\n');

                            foreach (string folderOrFile in stringSplit)
                            {
                                // Vytvoříme objekt FTP pro soubor nebo složku a přidáme ho do seznamu
                                FTP ftp = ParseFTPLine(ftpAddress, folderOrFile);
                                filesFTP.Add(ftp);
                            }

                            reader.Close();
                        }
                    }
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                // Zacházíme s výjimkou v případě chyby
                throw ex;
            }

            // Vrátíme pole FTP objektů
            return filesFTP.ToArray();
        }


        /// <summary>
        /// Analyzuje řádek obsahující vlastnosti souboru nebo složky na FTP serveru a vytváří objekt FTP.
        /// </summary>
        /// <param name="ftpAddress">Adresa FTP serveru.</param>
        /// <param name="folderOrFile">Řádek obsahující vlastnosti souboru nebo složky.</param>
        /// <returns>Objekt FTP reprezentující soubor nebo složku.</returns>
        private FTP ParseFTPLine(string ftpAddress, string folderOrFile)
        {
            var FTPFileProperties = folderOrFile.Split(' ');
            string date = "";
            string time = "";
            string size = "0"; // Výchozí hodnota pro velikost
            bool isFolder = folderOrFile.Contains("<DIR>");
            string name = "";

            for (int i = 0; i < folderOrFile.Length; i++)
            {
                if (i == 39)
                {
                    name = folderOrFile.Substring(i);
                }
            }

            int sizeCount = 0;
            foreach (string property in FTPFileProperties)
            {
                if (property != null && property.Contains("-") && date == "")
                {
                    date = property;
                }
                if (property != null && property.Contains(":") && time == "")
        {
                    time = property;
                }
                if (property != null && char.IsDigit(property[0]) && sizeCount < 3)
        {
                    sizeCount++;
                    size = isFolder ? "0" : property;
                }
            }

            DateTime? dateTime = null;
            try
            {
                // Analyzujeme datum a čas
                dateTime = DateTime.ParseExact($"{date} {time}", "MM-dd-yy hh:mmtt", new CultureInfo("en-US"));
            }
            catch { }

            string nameTmp = ftpAddress + name;

            // Vytvoříme objekt FTP pro soubor nebo složku a vrátíme ho
            FTP ftp = new FTP();
            ftp.Date = dateTime;
            ftp.Name = nameTmp.TrimEnd();
            ftp.Size = size;
            ftp.IsFolder = isFolder;

            return ftp;
        }
    }
}
