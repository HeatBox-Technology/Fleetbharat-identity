using System;


public class trans_accountTax
{
    public int TaxId { get; set; }

    public int AccountId { get; set; }

    public string CountryCode { get; set; }      // IN, US, AE

    public string TaxType { get; set; }           // VAT / GST / PAN / EIN

    public string TaxNumber { get; set; }

    public bool IsPrimary { get; set; }
}