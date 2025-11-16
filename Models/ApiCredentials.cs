namespace NopHoSoTuDong.Models
{
    public class ApiCredentials(string token = "", string groupId = "", string cookie = "")
    {
        public string Token { get; set; } = token;
        public string GroupId { get; set; } = groupId;
        public string Cookie { get; set; } = cookie;
        public override string ToString()
        {
            return $"Token={Token}, GroupId={GroupId}, Cookie={Cookie}";
        }
    }
}
