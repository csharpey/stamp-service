using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.X509;

namespace Rst.Pdf.Stamp;

public readonly struct SignatureInfo
{
    public SignatureInfo(SignedCms signedCms)
    {
        var certificate = signedCms.SignerInfos[0].Certificate;
        Debug.Assert(certificate != null);

        CertificateSerialNumber = certificate.SerialNumber;

        X509Certificate cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(certificate);
        DerSequence subject = cert.SubjectDN.ToAsn1Object() as DerSequence;
        DerSequence issuer = cert.IssuerDN.ToAsn1Object() as DerSequence;

        Debug.Assert(issuer != null);
        Debug.Assert(subject != null);

        var issuerLookup = ToLookup(issuer);

        var subjectLookup = ToLookup(subject);

        FullNameIssuer = issuerLookup[OidRecordHelper.Organization].FirstOrDefault();
        Name = subjectLookup[OidRecordHelper.CommonName].FirstOrDefault();
        Organization = subjectLookup[OidRecordHelper.Organization].FirstOrDefault();
        SurName = subjectLookup[OidRecordHelper.SurName].FirstOrDefault();
        GivenName = subjectLookup[OidRecordHelper.GivenName].FirstOrDefault();
        OGRN = subjectLookup[OidRecordHelper.OGRN].FirstOrDefault();

        var attrs = signedCms.SignerInfos[0].SignedAttributes
            .OfType<CryptographicAttributeObject>();
        SigningDate = new Pkcs9SigningTime();
        SigningDate.CopyFrom(attrs.First(a => a.Oid.Value == OidRecordHelper.SigningTime).Values[0]);

        Thumbprint = certificate.Thumbprint;
        StartDateCertificate = certificate.NotBefore.ToString();
        EndDateCertificate = certificate.NotAfter.ToString();
    }

    private static ILookup<string, string> ToLookup(DerSequence set)
    {
        return set.OfType<DerSet>().SelectMany(s => s.OfType<DerSequence>())
            .ToLookup(
                s => (s.OfType<Asn1Encodable>().First() as DerObjectIdentifier)?.Id,
                s => s[1].ToString());
    }


    public string SurName { get; }
    public string GivenName { get; }
    public string Organization { get; }
    public string Name { get; }
    public string CertificateSerialNumber { get; }
    public Pkcs9SigningTime SigningDate { get; }

    public string OGRN { get; }

    /// <summary>
    /// ФИО.
    /// </summary>
    public string FullNameSubject => string.Join(' ', SurName, GivenName);

    /// <summary>
    /// Издатель.
    /// </summary>
    public string FullNameIssuer { get; }

    /// <summary>
    /// Начало срока действия.
    /// </summary>
    public string StartDateCertificate { get; }

    /// <summary>
    /// Конец срока действия.
    /// </summary>
    public string EndDateCertificate { get; }

    /// <summary>
    /// Отпечаток сертификата
    /// </summary>
    public string Thumbprint { get; }
}