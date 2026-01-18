using System;
public class mst_account
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; }        // ACC-IND-0001
    public string AccountName { get; set; }
    public int Fk_userid { get; set; }
    public int? ParentAccountId { get; set; }
    public int CategoryId { get; set; }
    public string PrimaryDomain { get; set; } = "";
    public int CountryId { get; set; }   // ✅
    public int TaxTypeId { get; set; }   // ✅ country wise tax type
    public string HierarchyPath { get; set; }
    public bool Status { get; set; }              // ACTIVE / INACTIVE

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public DateTime? DeletedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public int? DeletedBy { get; set; }


}

