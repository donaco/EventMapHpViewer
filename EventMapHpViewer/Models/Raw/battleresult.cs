namespace EventMapHpViewer.Models.Raw
{
    public class battleresult
    {
        public battleresult_eventmap_result api_eventmap_result { get; set; }
    }

    public class battleresult_eventmap_result
    {
        public int api_max_hp { get; set; }
        public int api_now_hp { get; set; }
        public int api_sub_value { get; set; }
    }
}
