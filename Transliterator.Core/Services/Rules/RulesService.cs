using Transliterator.Domain.Interfaces;

namespace Transliterator.Core.Services.Rules
{
    public class RulesService
    {
        private readonly IProfileRepository _profileRepository;

        private readonly ArticleRules _articleRule;
        private readonly VowelRules _vowelRuleServies;
        private readonly WaslaRules _waslaRules;
        private readonly PostEmphaticVowelReplacer _postEmphaticVowelReplacer;
        private readonly LamRule _lamRule;
        private readonly AlifMaqsuraRule _alifMaqsuraRule;
        public RulesService(IProfileRepository profileRepository, ArticleRules articleRule, 
            VowelRules vowelRulesService, WaslaRules waslaRules,
            PostEmphaticVowelReplacer postEmphaticVowelReplacer, LamRule lamRule,
            AlifMaqsuraRule alifMaqsuraRule)
        {
            _profileRepository = profileRepository;
            _articleRule = articleRule;
            _vowelRuleServies = vowelRulesService;
            _waslaRules = waslaRules;
            _postEmphaticVowelReplacer = postEmphaticVowelReplacer;
            _lamRule = lamRule;
            _alifMaqsuraRule = alifMaqsuraRule;
        }

        public async Task<string> ApplyTajweedRulesAsync(string cyrillicText, string profileName = "Standard")
        {
            if (string.IsNullOrEmpty(cyrillicText))
                return cyrillicText;
            string result = cyrillicText;

            var profile = await _profileRepository.GetProfileAsync(profileName);
            if (profile == null)
                throw new Exception($"Profile {profileName} not found");
            
            result = _alifMaqsuraRule.ApplyAlifMaqsuraRule(result, profile);
            result = _waslaRules.ProcessWaslaAlif(result, profile);
            result = _articleRule.ApplySunMoonLettersRule(result, profile);
            result = _postEmphaticVowelReplacer.ApplyPostEmphaticVowelRule(result, profile);
            result = _lamRule.ApplyLamRule(result, profile);
            return result;
        }
    }
}