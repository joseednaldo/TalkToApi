using AutoMapper;
using TalkToApi.V1.Models;


namespace TalkToApi.Helpers
{
    public class DTOMapperProfile : Profile
    {
        public DTOMapperProfile()
        {

            /*
               AutoMapper = Objetivo de converte de um objeto pra outro.
             */
            //Convertendo  "palavra" para o objeto "palavraDTO"
            //CreateMap<Palavra, PalavraDTO>();
        }
    }
}
