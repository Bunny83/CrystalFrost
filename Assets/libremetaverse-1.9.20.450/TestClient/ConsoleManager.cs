using TMPro;

namespace Jenny
{ 

    // Start is called before the first frame update
    public static class Console
    {
        //public static TMP_InputField textInput;
        public static TMP_Text textOutput;

        public static void WriteLine(string input)
        {
            textOutput.text += "\n" + input;
        }
        public static void Write(string input)
        {
            textOutput.text += input;
        }        //public static void 

    }

}
