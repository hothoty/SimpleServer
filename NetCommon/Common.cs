using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// 서버와 클라이언트간에 공유할 내용입니다
namespace NetCommon
{
    public class Common
    {
        /// <summary>
        /// 콘솔창에서 비동기 방식으로 사용자 입력을 받기 위한 함수
        /// </summary>
        /// <returns></returns>
        static public async Task<string> ReadLineAsync()
        {
            var line = await Task.Run(() => Console.ReadLine());
            return line;
        }


        /// <summary>
        /// 콘솔창 공통 명령어 도움말 출력 함수
        /// </summary>
        static public void DisplayHelpCommand()
        {
            Console.WriteLine("/Cmd:  q(Quit) h(Help) stat(status info)");
        }
    }
}
