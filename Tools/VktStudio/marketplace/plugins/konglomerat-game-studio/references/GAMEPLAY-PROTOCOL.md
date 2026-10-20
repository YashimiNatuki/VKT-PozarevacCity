# Connected gameplay protocol

For each feature, write its entry trigger, authoritative state, legal transitions, input contract, failure/cancel path, visible/audio feedback and persistence boundary. Implement rules before presentation. Animation, AI text and audio cannot award money or advance missions independently.

Mission flow: offered -> accepted -> objective stages -> settlement -> completed. Stable mission/run identifiers guard settlement. Store reward receipt and resulting balance in one durable save transition. Loading an accepted run restores its objective and cargo without replaying settlement. Test duplicate completion, interruption, death and load at every durable boundary.

Training flow: nearby -> eligible -> active round -> recovery -> settlement -> exit. The authoritative model validates timing, stamina and round completion; visual taps do not directly award progress. Reject repeated input in the same phase. Distinguish bench strength from boxing endurance. Cancel on death, invalid location or incompatible activity; release owned camera, cursor, pause, input and audio. Do not silently resume partial rounds unless save semantics explicitly support it.

Police flow: witnessed event -> evidence with last known position -> search -> contact or loss of trail -> resolved consequences. Hearing a sound is not proof of seeing the player. Keep suspicion, wanted level and mission outcomes distinct. Test occlusion, stale evidence, de-escalation and repeat witnesses without multiplying the same penalty.

Optional AI flow: authored interaction -> bounded request -> timeout/cancel -> sanitized response or authored Serbian fallback. Never pass secrets, saves or arbitrary local files into a prompt. NPC dialogue must not execute commands or mutate authoritative rewards. Missing providers must not block gameplay.
