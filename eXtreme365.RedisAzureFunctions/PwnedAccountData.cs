namespace eXtreme365.RedisAzureFunctions
{
    public class PwnedAccountData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Domain { get; set; }
        public string BreachDate { get; set; }
        public string AddedDate { get; set; }
        public string ModifiedDate { get; set; }
        public int PwnCount { get; set; }
        public string Description { get; set; }
        public bool IsVerified { get; set; }
        public bool IsFabricated { get; set; }
        public bool IsSenstive { get; set; }
        public bool IsActive { get; set; }
        public bool IsRetired { get; set; }
        public bool IsSpamList { get; set; }
    }
}
