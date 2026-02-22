# SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
# SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
#
# SPDX-License-Identifier: MIT

# Same variant count as ru-RU (0..17); default and a few variants for English
tips-system-chat-message-wrap = { $variant ->
   [0] Tip: { $tip }
   [1] Read this: { $tip }
   [2] Simon says: { $tip }
   [11] Achtung! { $tip }
   [12] Did you know... { $tip }
  *[default] Tip: { $tip }
}
