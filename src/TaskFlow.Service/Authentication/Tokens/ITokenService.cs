using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskFlow.Service.Authentication.Tokens
{
    public interface ITokenService
    {
        string GenerateRefreshToken();
        string HashToken(string token);
    }
}
