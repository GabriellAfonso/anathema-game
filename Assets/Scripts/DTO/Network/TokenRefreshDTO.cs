[System.Serializable]
public class TokenRefreshRequestDTO
{
    public string refresh;
}

/// <summary>
/// Resposta do TokenRefreshView do SimpleJWT. O campo e 'access', nao 'token':
/// o LoginView do projeto renomeia para 'token' na resposta dele, o refresh
/// padrao nao.
/// </summary>
[System.Serializable]
public class TokenRefreshResponseDTO
{
    public string access;
}
