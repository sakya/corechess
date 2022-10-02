using CoreChess.Abstracts;

namespace CoreChess.Controls.Models;

public class SpinnerModel : BaseModel
{
    private string m_Message;
    public string Message
    {
        get => m_Message;
        set => SetField(ref m_Message, value);
    }
}