using System.Collections.Concurrent;
using System.Text.Json;
using Abstractions;

namespace LegendaryCMS.API
{
    /// <summary>
    /// AI Chatbot Manager for CMS
    /// Provides intelligent chat responses using AI language models
    /// </summary>
    public class AIChatbotManager
    {
        private readonly ConcurrentDictionary<string, ChatbotConversation> _conversations = new();
        private readonly ConcurrentDictionary<string, List<ChatbotMessage>> _conversationHistory = new();
        private IAILanguageModule? _aiLanguageModule;
        private IChatModule? _chatModule;
        private readonly object _lock = new();
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        // Chatbot configuration
        private readonly string _botName = "RaBot";
        private readonly int _maxHistoryLength = 50;
        private readonly string _systemPrompt = @"You are RaBot, a friendly and helpful AI assistant for the RaOS CMS platform. 

Your personality:
- Warm, approachable, and conversational
- Patient and understanding
- Encouraging and supportive
- Use natural, friendly language (not robotic)
- Show enthusiasm when helping users
- Ask follow-up questions to better understand needs
- Use emojis occasionally to be more personable

You help users with:
- Content management and publishing
- Forum and blog creation
- User authentication and permissions
- System configuration and setup
- General questions about the CMS features

Communication style:
- Start with a friendly greeting
- Acknowledge what the user is trying to do
- Provide clear, step-by-step guidance
- Offer to help with related tasks
- End with an invitation to ask more questions

Remember: You're having a conversation with a real person. Be human, be helpful, be friendly!";

        public void Initialize(object? moduleManager)
        {
            if (moduleManager != null)
            {
                // Try to get AI Language Module
                var modulesProperty = moduleManager.GetType().GetProperty("Modules");
                if (modulesProperty != null)
                {
                    var modules = modulesProperty.GetValue(moduleManager) as System.Collections.IEnumerable;
                    if (modules != null)
                    {
                        foreach (var moduleWrapper in modules)
                        {
                            var instanceProperty = moduleWrapper.GetType().GetProperty("Instance");
                            if (instanceProperty != null)
                            {
                                var instance = instanceProperty.GetValue(moduleWrapper);

                                if (instance is IAILanguageModule aiLang)
                                {
                                    _aiLanguageModule = aiLang;
                                }
                                else if (instance is IChatModule chat)
                                {
                                    _chatModule = chat;
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"[AIChatbot] Initialized - AI Module: {(_aiLanguageModule != null ? "Available" : "Unavailable")}");
        }

        /// <summary>
        /// Start a new chatbot conversation
        /// </summary>
        public async Task<ChatbotConversation> StartConversationAsync(string userId, string username)
        {
            await Task.CompletedTask;

            var conversationId = Guid.NewGuid().ToString();
            var conversation = new ChatbotConversation
            {
                ConversationId = conversationId,
                UserId = userId,
                Username = username,
                StartedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                MessageCount = 0,
                IsActive = true
            };

            _conversations[conversationId] = conversation;
            _conversationHistory[conversationId] = new List<ChatbotMessage>();

            // Add a friendly welcome message to start the conversation
            var welcomeMessage = new ChatbotMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Content = $"Hi {username}! 👋 Welcome to RaOS! I'm {_botName}, your friendly AI assistant.\n\n" +
                         "I'm here to help you get the most out of the CMS platform. Whether you're creating content, " +
                         "setting up forums, managing permissions, or just exploring - I've got your back! 😊\n\n" +
                         "What would you like to do today?",
                IsFromBot = true,
                Timestamp = DateTime.UtcNow
            };

            _conversationHistory[conversationId].Add(welcomeMessage);
            conversation.MessageCount++;

            Console.WriteLine($"[AIChatbot] Started conversation {conversationId} for user {username}");
            return conversation;
        }

        /// <summary>
        /// Send a message to the chatbot and get a response
        /// </summary>
        public async Task<ChatbotResponse> SendMessageAsync(string conversationId, string userId, string message)
        {
            if (!_conversations.TryGetValue(conversationId, out var conversation))
            {
                return new ChatbotResponse
                {
                    Success = false,
                    Error = "Conversation not found",
                    Timestamp = DateTime.UtcNow
                };
            }

            if (conversation.UserId != userId)
            {
                return new ChatbotResponse
                {
                    Success = false,
                    Error = "Unauthorized: User ID mismatch",
                    Timestamp = DateTime.UtcNow
                };
            }

            // Store user message
            var userMessage = new ChatbotMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Content = message,
                IsFromBot = false,
                Timestamp = DateTime.UtcNow
            };

            if (_conversationHistory.TryGetValue(conversationId, out var history))
            {
                history.Add(userMessage);

                // Trim history if too long
                if (history.Count > _maxHistoryLength)
                {
                    history.RemoveRange(0, history.Count - _maxHistoryLength);
                }
            }

            // Generate AI response
            string botReply;
            if (_aiLanguageModule != null)
            {
                try
                {
                    // Build context from conversation history
                    var context = BuildConversationContext(conversationId);

                    var aiResponse = await _aiLanguageModule.GenerateAsync(
                        intent: "chat_response",
                        context: context,
                        language: "en",
                        metadata: new Dictionary<string, object>
                        {
                            { "system_prompt", _systemPrompt },
                            { "user_message", message },
                            { "conversation_id", conversationId }
                        }
                    );

                    botReply = !string.IsNullOrWhiteSpace(aiResponse.Text) && string.IsNullOrEmpty(aiResponse.Error)
                        ? aiResponse.Text
                        : GetFallbackResponse(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AIChatbot] AI generation error: {ex.Message}");
                    botReply = GetFallbackResponse(message);
                }
            }
            else
            {
                // Use fallback responses when AI module is unavailable
                botReply = GetFallbackResponse(message);
            }

            // Store bot message
            var botMessage = new ChatbotMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                ConversationId = conversationId,
                Content = botReply,
                IsFromBot = true,
                Timestamp = DateTime.UtcNow
            };

            if (_conversationHistory.TryGetValue(conversationId, out var hist))
            {
                hist.Add(botMessage);
            }

            // Update conversation
            conversation.MessageCount += 2;
            conversation.LastActivityAt = DateTime.UtcNow;

            Console.WriteLine($"[AIChatbot] Processed message in conversation {conversationId}");

            return new ChatbotResponse
            {
                Success = true,
                ConversationId = conversationId,
                BotReply = botReply,
                MessageId = botMessage.MessageId,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Get conversation history
        /// </summary>
        public async Task<List<ChatbotMessage>> GetConversationHistoryAsync(string conversationId, string userId, int limit = 50)
        {
            await Task.CompletedTask;

            if (!_conversations.TryGetValue(conversationId, out var conversation))
            {
                return new List<ChatbotMessage>();
            }

            if (conversation.UserId != userId)
            {
                return new List<ChatbotMessage>();
            }

            if (!_conversationHistory.TryGetValue(conversationId, out var history))
            {
                return new List<ChatbotMessage>();
            }

            return history.TakeLast(limit).ToList();
        }

        /// <summary>
        /// Get all active conversations for a user
        /// </summary>
        public async Task<List<ChatbotConversation>> GetUserConversationsAsync(string userId)
        {
            await Task.CompletedTask;

            return _conversations.Values
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.LastActivityAt)
                .ToList();
        }

        /// <summary>
        /// End a conversation
        /// </summary>
        public async Task<bool> EndConversationAsync(string conversationId, string userId)
        {
            await Task.CompletedTask;

            if (!_conversations.TryGetValue(conversationId, out var conversation))
            {
                return false;
            }

            if (conversation.UserId != userId)
            {
                return false;
            }

            conversation.IsActive = false;
            conversation.EndedAt = DateTime.UtcNow;

            Console.WriteLine($"[AIChatbot] Ended conversation {conversationId}");
            return true;
        }

        /// <summary>
        /// Get chatbot statistics
        /// </summary>
        public async Task<ChatbotStats> GetStatsAsync()
        {
            await Task.CompletedTask;

            var activeConversations = _conversations.Values.Count(c => c.IsActive);
            var totalConversations = _conversations.Count;
            var totalMessages = _conversationHistory.Values.Sum(h => h.Count);

            return new ChatbotStats
            {
                ActiveConversations = activeConversations,
                TotalConversations = totalConversations,
                TotalMessages = totalMessages,
                AIModuleAvailable = _aiLanguageModule != null,
                BotName = _botName
            };
        }

        private string BuildConversationContext(string conversationId)
        {
            if (!_conversationHistory.TryGetValue(conversationId, out var history))
            {
                return _systemPrompt;
            }

            var contextBuilder = new System.Text.StringBuilder();
            contextBuilder.AppendLine(_systemPrompt);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Recent conversation:");

            // Include last 10 messages for context
            var recentMessages = history.TakeLast(10);
            foreach (var msg in recentMessages)
            {
                var speaker = msg.IsFromBot ? _botName : "User";
                contextBuilder.AppendLine($"{speaker}: {msg.Content}");
            }

            return contextBuilder.ToString();
        }

        private string GetFallbackResponse(string message)
        {
            var msgLower = message.ToLowerInvariant();

            // Greetings - warm and friendly
            if (msgLower.Contains("hello") || msgLower.Contains("hi") || msgLower.Contains("hey"))
            {
                return $"Hey there! 👋 I'm {_botName}, your friendly CMS assistant. I'm here to help you with anything related to the RaOS platform. What can I help you with today?";
            }

            // General help request
            if (msgLower.Contains("help") || msgLower.Contains("what can you do"))
            {
                var helpText = "I'd be happy to help! 😊 I can assist you with:\n\n" +
                       "📝 **Content Management** - Creating and editing content\n" +
                       "💬 **Forums & Blogs** - Setting up discussions and posts\n" +
                       "🔐 **Permissions** - Managing user access and roles\n" +
                       "⚙️ **Configuration** - System setup and settings\n";

                helpText += "\nWhat would you like to dive into?";
                return helpText;
            }

            // Understanding concepts
            if (msgLower.Contains("what is") || msgLower.Contains("explain") || msgLower.Contains("how does") ||
                msgLower.Contains("understand"))
            {
                return "I'm here to explain things clearly! 🧠 I break down complex topics into easy-to-understand pieces. " +
                       "What concept or feature would you like me to explain? The more specific you are, the better I can help!\n\n" +
                       "I can explain technical concepts, walk through processes step-by-step, or provide examples to illustrate ideas.";
            }

            // Forums
            if (msgLower.Contains("forum"))
            {
                return "Great question about forums! 🎯 Forums are perfect for building community discussions. " +
                       "To create a forum post, you'll use the `/api/forums/post` endpoint with proper authentication. " +
                       "You'll need the `ForumPost` permission to do this.\n\n" +
                       "Want me to walk you through the specific steps, or do you have questions about permissions?";
            }

            // Blogs
            if (msgLower.Contains("blog"))
            {
                return "Blogs are awesome for sharing content! ✍️ To create a blog post, use the `/api/blogs/create` endpoint. " +
                       "You'll need the right permissions (usually `BlogCreate`), but I can help you check that.\n\n" +
                       "Are you looking to create your first blog post, or do you need help with something specific?";
            }

            // Permissions
            if (msgLower.Contains("permission") || msgLower.Contains("access") || msgLower.Contains("role"))
            {
                return "Ah, permissions! 🔐 The CMS uses Role-Based Access Control (RBAC) with different permission levels. " +
                       "Think of it like different keys for different doors - each role has access to specific features.\n\n" +
                       "If you need to adjust your permissions, your administrator can help with that. " +
                       "Would you like to know more about what each role can do?";
            }

            // API questions
            if (msgLower.Contains("api") || msgLower.Contains("endpoint"))
            {
                return "Good question about the API! 🔌 The CMS has a comprehensive REST API with lots of endpoints. " +
                       "You can check out all available endpoints at `/api/endpoints` to see the full list.\n\n" +
                       "Each endpoint has its own purpose - some for forums, some for blogs, others for content management. " +
                       "Is there a specific type of API call you're trying to make?";
            }

            // Create/Add content
            if (msgLower.Contains("create") || msgLower.Contains("add") || msgLower.Contains("new"))
            {
                return "I can definitely help you create something! 🎨 Are you looking to:\n" +
                       "- Create a forum post?\n" +
                       "- Add a blog article?\n" +
                       "- Set up a new content page?\n" +
                       "- Add a new user?\n\n" +
                       "Let me know what you'd like to create, and I'll guide you through it!";
            }

            // Errors or problems
            if (msgLower.Contains("error") || msgLower.Contains("problem") || msgLower.Contains("not work") ||
                msgLower.Contains("issue") || msgLower.Contains("broken"))
            {
                return "Oh no! 😟 Sorry to hear you're running into issues. I'm here to help troubleshoot! " +
                       "Can you tell me a bit more about what's happening? The more details you share, the better I can assist you.\n\n" +
                       "For example: What were you trying to do? What error message did you see?";
            }

            // Thank you
            if (msgLower.Contains("thank") || msgLower.Contains("thanks"))
            {
                return "You're very welcome! 😊 I'm always here if you need anything else. " +
                       "Don't hesitate to reach out - helping you is what I'm here for!";
            }

            // Goodbye
            if (msgLower.Contains("bye") || msgLower.Contains("goodbye") || msgLower.Contains("see you"))
            {
                return "Take care! 👋 Feel free to come back anytime you have questions. " +
                       "I'm always here to help with your CMS needs. Have a great day!";
            }

            // Default - encouraging and conversational
            var defaultResponse = $"I'm {_botName}, your friendly CMS assistant! 🤖 I'm here to help you with anything related to the RaOS platform.\n\n" +
                   "I can help with:\n" +
                   "• Managing content and pages\n" +
                   "• Setting up forums and blogs\n" +
                   "• Understanding permissions and roles\n" +
                   "• API usage and endpoints\n" +
                   "• General configuration\n";

            defaultResponse += "\nWhat would you like to know? Feel free to ask me anything in your own words!";
            return defaultResponse;
        }
    }

    /// <summary>
    /// Represents a chatbot conversation
    /// </summary>
    public class ChatbotConversation
    {
        public string ConversationId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int MessageCount { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Represents a chatbot message
    /// </summary>
    public class ChatbotMessage
    {
        public string MessageId { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsFromBot { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Chatbot response structure
    /// </summary>
    public class ChatbotResponse
    {
        public bool Success { get; set; }
        public string? ConversationId { get; set; }
        public string? BotReply { get; set; }
        public string? MessageId { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Chatbot statistics
    /// </summary>
    public class ChatbotStats
    {
        public int ActiveConversations { get; set; }
        public int TotalConversations { get; set; }
        public int TotalMessages { get; set; }
        public bool AIModuleAvailable { get; set; }
        public string BotName { get; set; } = string.Empty;
    }
}
