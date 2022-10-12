using BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Helpers.HtmlToPdfConverter
{
    public class TemplateGenerator
    {
        public static string GetHTMLString(List<GroupedMessagesDTO> groupedMessagesDTOs)
        {
            var sb = new StringBuilder();
            sb.Append(@"
                        <html>
                            <head>
                            </head>
                            <body>
                                <div class='header'><h1>Messages report</h1></div>
                                <ul>"
            );
            foreach (var user in groupedMessagesDTOs)
            {
                sb.AppendFormat(@"<li>{0}</li>
                                  <li>{1} </li>
                                      <ul>",
                        user.UserName,
                        user.PhoneNumber);
                foreach (var message in user.Messages)
                {
                    sb.AppendFormat(@"      <li>{0}</li>
                                            <li>{1}</li>
                                            <li>{2} <br/><br/>  </li>",
                        message.ReadStatus,
                        message.CreateDate,
                        message.Body);
                }
                sb.Append(@" </ul>");
            }
            sb.Append(@"
                                <br/><br/></ul>
                            </body>
                        </html>");
            return sb.ToString();
        }
    }
}
