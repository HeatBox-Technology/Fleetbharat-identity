using System;
public class mst_account
{
    public int AccountId { get; set; }

    public string AccountCode { get; set; }        // ACC-IND-0001

    public string AccountName { get; set; }

    public string AccountType { get; set; }        // OEM / DISTRIBUTOR / ENTERPRISE / DEALER

    public int Fk_userid { get; set; }

    public int? ParentAccountId { get; set; }

    public string HierarchyPath { get; set; }

    public string PrimaryDomain { get; set; }

    public string Status { get; set; }              // ACTIVE / INACTIVE

    public DateTime CreatedOn { get; set; }

}

