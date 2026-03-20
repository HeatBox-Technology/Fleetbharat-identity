using System;
using System.Collections.Generic;

public class mst_user
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string EmailId { get; set; }
    public string MobileNo { get; set; }
    public int UserStatusLkpId { get; set; }
    public List<mst_role> Roles { get; set; }
}