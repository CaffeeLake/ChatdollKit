﻿using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ChatdollKit.Dialog.Processor
{
    public class ChatGPTRouter : SkillRouterBase
    {
        private ChatGPTService chatGPT;
        private Dictionary<string, string> functionResolver = new Dictionary<string, string>();

        private void Awake()
        {
            chatGPT = GetComponent<ChatGPTService>();
        }

        public override void RegisterSkill(ISkill skill)
        {
            base.RegisterSkill(skill);
            if (skill is ChatGPTFunctionSkillBase)
            {
                var func = ((ChatGPTFunctionSkillBase)skill).GetFunctionSpec();
                chatGPT.AddFunction(func);
                functionResolver.Add(func.name, skill.TopicName);
            }
        }

        public override async UniTask<IntentExtractionResult> ExtractIntentAsync(Request request, State state, CancellationToken token)
        {
            var messages = new List<ChatGPTMessage>();

            // System
            if (!string.IsNullOrEmpty(chatGPT.SystemMessageContent))
            {
                messages.Add(new ChatGPTMessage("system", chatGPT.SystemMessageContent));
            }
            // Histories
            messages.AddRange(chatGPT.GetHistories(state));
            // User (current input)
            messages.Add(new ChatGPTMessage("user", request.Text));

            request.Payloads.Add(chatGPT.ChatCompletionAsync(messages));

            var functionName = await chatGPT.WaitForFunctionName(token);

            if (!string.IsNullOrEmpty(functionName))
            {
                if (functionResolver.ContainsKey(functionName))
                {
                    return new IntentExtractionResult(new Intent(functionResolver[functionName], Priority.Highest, true));
                }
            }

            return new IntentExtractionResult("chatgptcontent", Priority.Normal);
        }
    }
}
