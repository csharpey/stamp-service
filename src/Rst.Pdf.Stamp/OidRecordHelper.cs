using System.Security.Cryptography;

namespace Rst.Pdf.Stamp
{
    public static class OidRecordHelper
    {
        public static readonly string Organization = new Oid("O").Value;
        public static readonly string CommonName = new Oid("CN").Value;
        public static readonly string SurName = new Oid("SN").Value;
        public static readonly string GivenName = new Oid("G").Value;
        public static readonly string OGRN = new Oid("OGRN").Value;
        public static readonly string SigningTime = new Oid("signingTime").Value;
    }
}