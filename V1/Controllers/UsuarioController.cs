﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using TalkToApi.V1.Models;
using System.Text;
using System.Security.Claims;
using TalkToApi.V1.Repositories.Contracts;
using TalkToApi.V1.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using TalkToApi.Helpers.Constants;
using Microsoft.AspNetCore.Cors;

namespace TalkToApi.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [EnableCors("AnyOrigin")] 
    public class UsuarioController : ControllerBase
    {
        private readonly IMapper _maper;
        private readonly ITokenRepository _tokenRepositorie;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsuarioController(IUsuarioRepository usuarioRepository, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ITokenRepository tokenRepositorie, IMapper maper)
        {

            _usuarioRepository = usuarioRepository;
            _signInManager = signInManager;
            _userManager = userManager;
            _tokenRepositorie = tokenRepositorie;
            _maper = maper;
        }
        
       [Authorize]
       [HttpGet("",Name = "UsuarioObterTodos")]
       [DisableCors()]
        public ActionResult ObterTodos([FromHeader(Name = "Accept")] string mediaType)
        {

            var usuarioAppUser = _userManager.Users.ToList();

            if (mediaType == CustomMediaType.Hateoas)
            {

                var listaUsuarioDTO = _maper.Map<List<ApplicationUser>, List<UsuarioDTO>>(usuarioAppUser);

                foreach (var UsuarioDTO in listaUsuarioDTO)
                {
                    UsuarioDTO.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObter", new { id = UsuarioDTO.Id }), "GET"));
                }

                var lista = new ListaDTO<UsuarioDTO>() { Lista = listaUsuarioDTO };
                lista.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObterTodos", null), "GET"));
                return Ok(lista);
            }
            else
            {
                //todo - Automapper -> converter para objeto sem Hyperlink
                var usuarioresultado = _maper.Map<List<ApplicationUser>, List<UsuarioDTOSemHyperLink>>(usuarioAppUser);
                return Ok(usuarioAppUser);
            }


        }


        [Authorize]
        [HttpGet("{id}", Name = "UsuarioObter")]
        public ActionResult ObterUsuario(string id, [FromHeader(Name = "Accept")] string mediaType)
        {
            var usuario = _userManager.FindByIdAsync(id).Result;

            if (usuario == null)
                return NotFound();

            if (mediaType == CustomMediaType.Hateoas)
            {

                //USANSO HETEOS-links
                var usuarioDTOdb = _maper.Map<ApplicationUser, UsuarioDTO>(usuario);
                usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));
                usuarioDTOdb.Links.Add(new LinkDTO("_atualizar", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));
                return Ok(usuarioDTOdb);
            }else
            {
                //todo - Automapper -> converter para objeto sem Hyperlink
                var usuarioresultado = _maper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                return Ok(usuarioresultado);
            }
        }


        [HttpPost("login")]
        public ActionResult Login([FromBody]UsuarioDTO usuarioDTO)
        {
            //removendo os campos que não vamos validar no mommento de fazer o login.
            ModelState.Remove("ConfirmacaoSenha");
            ModelState.Remove("Nome");

            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _usuarioRepository.Obter(usuarioDTO.Email, usuarioDTO.Senha);
                if (usuario != null)
                {
                    #region  Login usando Idenity

                    /* não é mais necessario.
                     ele utiliza o CookieBuider pra guarda os dados do usuario logado.
                     vamos usar agora o jtw que nao guarda estado.
                    */
                    //_signInManager.SignInAsync(usuario, false);    

                    #endregion
                    return CriarNovoToken(usuario);

                }
                else
                {
                    return NotFound("Usuário não localizado!!!");
                }
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        private TokenDTO BuildToken(ApplicationUser usuario)
        {
            var claims = new[]
            {
               new Claim(JwtRegisteredClaimNames.Email,usuario.Email),
               new Claim(JwtRegisteredClaimNames.Sub,usuario.Id)   //propriedade "Sub" serve para idenfificar o usuario.
            };

            #region  Cria chave

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("chave-api-jwt-minhas-tarefas")); //O ideal é criar a chave(texto) no appsettings.js
            var sign = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);                       // criando assinatura
            var exp = DateTime.UtcNow.AddHours(1);  // UtcNow NAO FICO REFEM DO FUSO HORARIO   // validade de uma hora.

            /*Classe que gera o token.*/
            JwtSecurityToken token = new JwtSecurityToken(
                 issuer: null,  // Não quero dizer quem esta emitindo o token    TRADUTOR: issuer => EMISSOR/EMISSORA/EMITENTENTE
                 audience: null, // Não importa quem vai consumir o token    - Pra quem é esse token, quem vai consumir esse token... ex: qual dominio.
                 claims: claims,
                 expires: exp,
                 signingCredentials: sign

                );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var refleshToken = Guid.NewGuid().ToString();
            var expRefleshToken = DateTime.UtcNow.AddHours(2);

            var tokenDTO = new TokenDTO
            {
                Token = tokenString,
                Expiration = exp,
                RefreshToken = refleshToken,
                ExpirationRefreshToken = expRefleshToken

            };

            return tokenDTO;
            #endregion
        }

        [HttpPost("renovar")]
        public ActionResult Renovar([FromBody]TokenDTO _tokenDTO)
        {
            var refleshTokneDB = _tokenRepositorie.Obter(_tokenDTO.RefreshToken);
            if (refleshTokneDB == null)
                return NotFound();

            #region  Pegar Antigo  RefleshToken  =>  Atualizar ou Seja vamos Destiva-lo.
            refleshTokneDB.Atualizado = DateTime.Now;
            refleshTokneDB.Utilizado = true;
            _tokenRepositorie.Atualizar(refleshTokneDB);
            #endregion

            #region Gerar um novo token e Salvar no banco de dados..
            var usuario = _usuarioRepository.Obter(refleshTokneDB.UsuarioId);
            return (CriarNovoToken(usuario));
            #endregion

        }

        [HttpPost("",Name= "UsuarioCadastrar")]//rota padrao
        public ActionResult Cadastrar([FromBody]UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _maper.Map<UsuarioDTO, ApplicationUser>(usuarioDTO); //new ApplicationUser();
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;

                var resultado = _userManager.CreateAsync(usuario, usuarioDTO.Senha).Result;

                if (!resultado.Succeeded)
                {
                    //StringBuilder sb = new StringBuilder();
                    List<string> erros = new List<string>();
                    foreach (var erro in resultado.Errors)
                    {
                        erros.Add(erro.Description);
                    }

                    return UnprocessableEntity(erros);

                }
                else
                {

                    if (mediaType == CustomMediaType.Hateoas)
                    {
                        var usuarioDTOdb = _maper.Map<ApplicationUser, UsuarioDTO>(usuario);
                        usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioCadastrar", new { id = usuario.Id }), "POST"));
                        usuarioDTOdb.Links.Add(new LinkDTO("_obter", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));
                        usuarioDTOdb.Links.Add(new LinkDTO("_atualizar", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));
                        return Ok(usuarioDTOdb);
                    }else
                    {
                        //todo - Automapper -> converter para objeto sem Hyperlink
                        var usuarioresultado = _maper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                        return Ok(usuarioresultado);
                    }

                }
                
            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        /*
         * api/usuario/{id} -> PRO SE TRATAR DE ATUALIZAÇÃO USAMOS O "PUT".   
         */
        [Authorize]
        [HttpPut("{id}",Name ="UsuarioAtualizar")]
        public ActionResult Atualizar(string id,[FromBody]UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")] string mediaType)
        {
            //if (_userManager.GetUserAsync(HttpContext.User).Result.Id != id)
            //PODES USAR FILTER NESSA REGRA PRA NAO FICAR REPETINDO ...
            ApplicationUser usuario = _userManager.GetUserAsync(HttpContext.User).Result;
            if(usuario.Id != id)
            {
                return Forbid();
            }
            if (ModelState.IsValid)
            {
                // TODO - REFATORAR PARA AUTOMAPER
               // ApplicationUser usuario = new ApplicationUser();
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
                usuario.Slogan = usuario.Slogan;


                // TODO - REMOVER NO IDENTITY OS CRITÉRIOS DA SENHA
                var resultado = _userManager.UpdateAsync(usuario).Result;
                _userManager.RemovePasswordAsync(usuario);
                _userManager.AddPasswordAsync(usuario,usuarioDTO.Senha);

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var erro in resultado.Errors)
                    {
                        erros.Add(erro.Description);
                    }

                    return UnprocessableEntity(erros);
                }
                else
                {
                    if (mediaType == CustomMediaType.Hateoas)
                    {
                        var usuarioDTOdb = _maper.Map<ApplicationUser, UsuarioDTO>(usuario);
                        usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));
                        usuarioDTOdb.Links.Add(new LinkDTO("_obter", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));
                        return Ok(usuarioDTOdb);
                    }else
                    {
                        //todo - Automapper -> converter para objeto sem Hyperlink
                        var usuarioresultado = _maper.Map<ApplicationUser, UsuarioDTOSemHyperLink>(usuario);
                        return Ok(usuarioresultado);
                    }

                }

            }
            else
            {
                return UnprocessableEntity(ModelState);
            }
        }

        #region  Métodos privados
        private ActionResult CriarNovoToken(ApplicationUser usuario)
        {

            //Trabalhando com  token (JWT)
            var token = BuildToken(usuario);

            //salvar o token no banco.
            var tokenModel = new Token()
            {
                RefreshToken = token.RefreshToken,
                ExpirationToken = token.Expiration,
                ExpirationRefreshToken = token.ExpirationRefreshToken,
                Usuario = usuario,
                Criado = DateTime.Now,
                Utilizado = false
            };
            _tokenRepositorie.Cadastrar(tokenModel);

            return Ok(token);
        }
        #endregion

    }
}