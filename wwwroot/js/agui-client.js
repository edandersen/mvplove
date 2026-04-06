/**
 * Plain JS client for an AG-UI endpoint.
 * Sends RunAgentInput as POST, reads back SSE events.
 */
(function () {
  'use strict';

  // ── State ──────────────────────────────────────────────────────────
  let threadId = crypto.randomUUID();
  let messages = [];         // AG-UI message history
  let currentAssistantText = '';
  let currentMessageId = null;
  let isRunning = false;
  let abortController = null;

  // ── DOM refs (set in init) ─────────────────────────────────────────
  let chatMessages, chatInput, sendBtn, chatForm;

  // ── Markdown rendering ──────────────────────────────────────────────
  function renderMarkdown(text) {
    if (typeof marked !== 'undefined') {
      return marked.parse(text, { breaks: true });
    }
    return text.replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/\n/g, '<br>');
  }

  // ── Helpers ────────────────────────────────────────────────────────
  function scrollToBottom() {
    chatMessages.scrollTop = chatMessages.scrollHeight;
  }

  function setInputEnabled(enabled) {
    chatInput.disabled = !enabled;
    sendBtn.disabled = !enabled;
    if (enabled) chatInput.focus();
  }

  function appendMessageBubble(role, text) {
    const wrapper = document.createElement('div');
    wrapper.className = role === 'user'
      ? 'flex justify-end'
      : 'flex justify-start';

    const bubble = document.createElement('div');
    bubble.className = role === 'user'
      ? 'max-w-[75%] rounded-2xl px-4 py-2 bg-blue-600 text-white text-sm whitespace-pre-wrap'
      : 'max-w-[75%] rounded-2xl px-4 py-2 bg-slate-200 text-slate-800 text-sm prose-chat';
    if (role === 'assistant') {
      bubble.innerHTML = renderMarkdown(text);
    } else {
      bubble.textContent = text;
    }

    wrapper.appendChild(bubble);
    chatMessages.appendChild(wrapper);
    scrollToBottom();
    return bubble;
  }

  function getOrCreateAssistantBubble(messageId) {
    let bubble = document.getElementById('msg-' + messageId);
    if (!bubble) {
      const wrapper = document.createElement('div');
      wrapper.className = 'flex justify-start';
      bubble = document.createElement('div');
      bubble.id = 'msg-' + messageId;
      bubble.className = 'max-w-[75%] rounded-2xl px-4 py-2 bg-slate-200 text-slate-800 text-sm prose-chat';
      wrapper.appendChild(bubble);
      chatMessages.appendChild(wrapper);
    }
    return bubble;
  }

  // ── SSE line parser ────────────────────────────────────────────────
  // fetch-based SSE since EventSource only supports GET.
  function parseSSE(text) {
    const events = [];
    const blocks = text.split('\n\n');
    for (const block of blocks) {
      if (!block.trim()) continue;
      let eventType = '';
      let data = '';
      for (const line of block.split('\n')) {
        if (line.startsWith('event:')) {
          eventType = line.slice(6).trim();
        } else if (line.startsWith('data:')) {
          data += line.slice(5).trim();
        }
      }
      if (data) {
        try {
          events.push({ event: eventType, data: JSON.parse(data) });
        } catch { /* skip malformed */ }
      }
    }
    return events;
  }

  // ── Handle a single AG-UI event ────────────────────────────────────
  function handleEvent(evt) {
    const d = evt.data;
    switch (d.type) {
      case 'RUN_STARTED':
        break;

      case 'TEXT_MESSAGE_START':
        currentMessageId = d.messageId;
        currentAssistantText = '';
        getOrCreateAssistantBubble(d.messageId);
        break;

      case 'TEXT_MESSAGE_CONTENT': {
        currentAssistantText += d.delta;
        const bubble = getOrCreateAssistantBubble(d.messageId);
        bubble.innerHTML = renderMarkdown(currentAssistantText);
        scrollToBottom();
        break;
      }

      case 'TEXT_MESSAGE_END':
        if (currentMessageId) {
          const bubble = getOrCreateAssistantBubble(currentMessageId);
          bubble.innerHTML = renderMarkdown(currentAssistantText);
          messages.push({
            id: currentMessageId,
            role: 'assistant',
            content: currentAssistantText,
          });
        }
        currentMessageId = null;
        currentAssistantText = '';
        break;

      case 'RUN_FINISHED':
        isRunning = false;
        setInputEnabled(true);
        break;

      case 'RUN_ERROR':
        isRunning = false;
        appendMessageBubble('assistant', '⚠️ Error: ' + (d.message || 'Unknown error'));
        setInputEnabled(true);
        break;

      default:
        // Ignore other event types for now
        break;
    }
  }

  // ── Send a message ─────────────────────────────────────────────────
  async function sendMessage(text) {
    if (isRunning || !text.trim()) return;

    isRunning = true;
    setInputEnabled(false);

    const userMsg = {
      id: crypto.randomUUID(),
      role: 'user',
      content: text.trim(),
    };
    messages.push(userMsg);
    appendMessageBubble('user', userMsg.content);

    const runId = crypto.randomUUID();
    const body = {
      threadId: threadId,
      runId: runId,
      messages: messages,
      tools: [],
      context: [],
      state: {},
      forwardedProps: {},
    };

    abortController = new AbortController();

    try {
      const response = await fetch('/mvpcopilot', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'text/event-stream',
        },
        body: JSON.stringify(body),
        signal: abortController.signal,
      });

      if (!response.ok) {
        throw new Error('Server responded with ' + response.status);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        // Process complete SSE blocks (separated by double newline)
        const parts = buffer.split('\n\n');
        // Keep the last part as it may be incomplete
        buffer = parts.pop() || '';

        for (const part of parts) {
          if (!part.trim()) continue;
          const events = parseSSE(part + '\n\n');
          for (const evt of events) {
            handleEvent(evt);
          }
        }
      }

      // Process any remaining buffer
      if (buffer.trim()) {
        const events = parseSSE(buffer + '\n\n');
        for (const evt of events) {
          handleEvent(evt);
        }
      }
    } catch (err) {
      if (err.name !== 'AbortError') {
        appendMessageBubble('assistant', '⚠️ ' + err.message);
      }
    } finally {
      isRunning = false;
      setInputEnabled(true);
      abortController = null;
    }
  }

  // ── Init ───────────────────────────────────────────────────────────
  function init() {
    chatMessages = document.getElementById('agui-messages');
    chatInput = document.getElementById('agui-input');
    sendBtn = document.getElementById('agui-send');
    chatForm = document.getElementById('agui-form');

    if (!chatMessages || !chatInput || !sendBtn || !chatForm) return;

    chatForm.addEventListener('submit', function (e) {
      e.preventDefault();
      const text = chatInput.value;
      chatInput.value = '';
      sendMessage(text);
    });

    chatInput.focus();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
