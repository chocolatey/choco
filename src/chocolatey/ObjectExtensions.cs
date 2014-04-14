namespace chocolatey
{
    public static class ObjectExtensions {

        public static string to_string(this object input)
        {
            if (input == null) return string.Empty;

            return input.ToString();
        }
    }
}