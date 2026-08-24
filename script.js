document.addEventListener('DOMContentLoaded', () => {
    'use strict';

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;
    const homePage = document.body?.dataset.page === 'home';
    const navbar = document.querySelector('.navbar');
    const navToggle = document.querySelector('.nav-toggle');
    const progress = document.getElementById('scroll-progress');

    const updateChrome = () => {
        const scrollTop = window.scrollY || document.documentElement.scrollTop;
        const scrollable = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
        navbar?.classList.toggle('scrolled', scrollTop > 26);
        if (progress) progress.style.width = `${Math.min(100, (scrollTop / scrollable) * 100)}%`;
    };
    updateChrome();
    window.addEventListener('scroll', updateChrome, { passive: true });

    navToggle?.addEventListener('click', () => {
        const open = navbar.classList.toggle('menu-open');
        navToggle.setAttribute('aria-expanded', String(open));
    });
    document.querySelectorAll('.nav-links a').forEach(link => link.addEventListener('click', () => {
        navbar?.classList.remove('menu-open');
        navToggle?.setAttribute('aria-expanded', 'false');
    }));

    // Use a same-origin Blob download as a compatibility fallback. Some
    // browsers ignore the HTML download attribute when the response is served
    // through a static edge asset, even when Content-Disposition is present.
    document.querySelectorAll('a.package-download').forEach(link => {
        link.addEventListener('click', async event => {
            if (!window.fetch || !window.URL?.createObjectURL) return;
            event.preventDefault();
            const originalLabel = link.textContent;
            link.setAttribute('aria-busy', 'true');
            link.style.pointerEvents = 'none';
            try {
                const response = await fetch(link.href, { credentials: 'same-origin' });
                if (!response.ok) throw new Error(`Download failed: ${response.status}`);
                const blobUrl = URL.createObjectURL(await response.blob());
                const fileLink = document.createElement('a');
                fileLink.href = blobUrl;
                fileLink.download = link.getAttribute('download') || link.pathname.split('/').pop() || 'block.zip';
                document.body.appendChild(fileLink);
                fileLink.click();
                fileLink.remove();
                window.setTimeout(() => URL.revokeObjectURL(blobUrl), 1500);
            } catch (error) {
                console.error(error);
                window.location.assign(link.href);
            } finally {
                link.textContent = originalLabel;
                link.removeAttribute('aria-busy');
                link.style.pointerEvents = '';
            }
        });
    });

    const reveals = document.querySelectorAll('.reveal');
    if (reducedMotion || !('IntersectionObserver' in window)) {
        reveals.forEach(element => element.classList.add('visible'));
    } else {
        const observer = new IntersectionObserver((entries, instance) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('visible');
                instance.unobserve(entry.target);
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -4% 0px' });
        reveals.forEach(element => observer.observe(element));
    }

    if (!reducedMotion) {
        document.querySelectorAll('.scramble-text').forEach(element => {
            const target = element.dataset.text || element.textContent;
            const glyphs = '!<>-_\\/[]{}+=*^?#';
            const original = target.split('');
            let frame = 0;
            const total = Math.min(48, Math.max(26, target.length));
            const animate = () => {
                element.textContent = original.map((character, index) => {
                    if (character === ' ') return ' ';
                    const threshold = (index / original.length) * total;
                    return frame > threshold ? character : glyphs[Math.floor(Math.random() * glyphs.length)];
                }).join('');
                frame += 1;
                if (frame <= total + 4) requestAnimationFrame(animate);
                else element.textContent = target;
            };
            window.setTimeout(animate, 360);
        });
    }

    document.querySelectorAll('.tilt-card').forEach(card => {
        if (!finePointer || reducedMotion || homePage) return;
        card.addEventListener('pointermove', event => {
            const rect = card.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const y = event.clientY - rect.top;
            card.style.setProperty('--mouse-x', `${x}px`);
            card.style.setProperty('--mouse-y', `${y}px`);
            const rotateX = ((y / rect.height) - 0.5) * -5;
            const rotateY = ((x / rect.width) - 0.5) * 5;
            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
        });
        card.addEventListener('pointerleave', () => { card.style.transform = ''; });
    });

    document.querySelectorAll('.magnetic').forEach(button => {
        if (!finePointer || reducedMotion) return;
        button.addEventListener('pointermove', event => {
            const rect = button.getBoundingClientRect();
            const x = event.clientX - rect.left - rect.width / 2;
            const y = event.clientY - rect.top - rect.height / 2;
            button.style.transform = `translate(${x * 0.08}px, ${y * 0.13}px)`;
        });
        button.addEventListener('pointerleave', () => { button.style.transform = ''; });
    });

    const aura = document.getElementById('cursor-aura');
    if (aura && finePointer && !homePage) {
        window.addEventListener('pointermove', event => {
            aura.style.left = `${event.clientX}px`;
            aura.style.top = `${event.clientY}px`;
            aura.style.opacity = '1';
        }, { passive: true });
        document.documentElement.addEventListener('mouseleave', () => { aura.style.opacity = '0'; });
    }

    if (finePointer && !homePage) {
        const customCursor = document.createElement('div');
        customCursor.id = 'custom-cursor';
        customCursor.setAttribute('aria-hidden', 'true');
        document.body.appendChild(customCursor);
        window.addEventListener('pointermove', event => {
            customCursor.style.transform = `translate3d(${event.clientX}px, ${event.clientY}px, 0)`;
            customCursor.style.opacity = '1';
        }, { passive: true });
        document.documentElement.addEventListener('mouseleave', () => { customCursor.style.opacity = '0'; });
        document.documentElement.addEventListener('mouseenter', () => { customCursor.style.opacity = '1'; });
    }

    document.querySelectorAll('[data-count]').forEach(counter => {
        const target = Number(counter.dataset.count || 0);
        const suffix = counter.dataset.suffix || '';
        if (reducedMotion || !('IntersectionObserver' in window)) {
            counter.textContent = `${target}${suffix}`;
            return;
        }
        const countObserver = new IntersectionObserver(entries => {
            if (!entries[0].isIntersecting) return;
            const start = performance.now();
            const duration = 900;
            const tick = now => {
                const progressValue = Math.min(1, (now - start) / duration);
                const eased = 1 - Math.pow(1 - progressValue, 3);
                counter.textContent = `${Math.round(target * eased)}${suffix}`;
                if (progressValue < 1) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
            countObserver.disconnect();
        }, { threshold: .8 });
        countObserver.observe(counter);
    });

    document.querySelectorAll('.faq-question').forEach(button => {
        button.addEventListener('click', () => {
            button.classList.toggle('active');
            const answer = button.nextElementSibling;
            if (answer) answer.style.maxHeight = button.classList.contains('active') ? `${answer.scrollHeight}px` : '0px';
        });
    });

    const docsSearch = document.querySelector('.docs-search input');
    if (docsSearch) {
        const chapterLinks = [...document.querySelectorAll('.docs-link')];
        docsSearch.addEventListener('input', () => {
            const query = docsSearch.value.trim().toLowerCase();
            chapterLinks.forEach(link => {
                link.style.display = !query || link.textContent.toLowerCase().includes(query) ? '' : 'none';
            });
        });
    }

    document.querySelectorAll('.copy-code').forEach(button => {
        button.addEventListener('click', async () => {
            const code = button.closest('.docs-code')?.querySelector('code')?.innerText || '';
            if (!code) return;
            try {
                await navigator.clipboard.writeText(code);
            } catch {
                const field = document.createElement('textarea');
                field.value = code;
                field.setAttribute('readonly', '');
                field.style.position = 'fixed';
                field.style.opacity = '0';
                document.body.appendChild(field);
                field.select();
                document.execCommand('copy');
                field.remove();
            }
            button.textContent = 'Copied';
            button.classList.add('copied');
            window.setTimeout(() => {
                button.textContent = 'Copy';
                button.classList.remove('copied');
            }, 1400);
        });
    });

    const docsToc = document.querySelector('.docs-toc');
    if (docsToc) {
        const tocLinks = [...docsToc.querySelectorAll('a[href^="#"]')];
        const sections = tocLinks.map(link => document.querySelector(link.getAttribute('href'))).filter(Boolean);
        const setActiveToc = id => {
            tocLinks.forEach(link => link.classList.toggle('active', link.getAttribute('href') === `#${id}`));
        };
        if ('IntersectionObserver' in window) {
            const tocObserver = new IntersectionObserver(entries => {
                const visible = entries.filter(entry => entry.isIntersecting).sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
                if (visible[0]) setActiveToc(visible[0].target.id);
            }, { rootMargin: '-18% 0px -65% 0px', threshold: 0 });
            sections.forEach(section => tocObserver.observe(section));
        }
        const updateDocsProgress = () => {
            const article = document.querySelector('.docs-article');
            if (!article) return;
            const rect = article.getBoundingClientRect();
            const total = Math.max(1, article.offsetHeight - window.innerHeight);
            const travelled = Math.min(total, Math.max(0, -rect.top + 120));
            docsToc.style.setProperty('--toc-progress', `${(travelled / total) * 100}%`);
        };
        updateDocsProgress();
        window.addEventListener('scroll', updateDocsProgress, { passive: true });
    }

    const terminal = document.getElementById('term-typing');
    if (terminal) {
        const lines = [
            { text: 'block customer-report.blk', type: 'command', wait: 500 },
            { text: '[System] Execution plan created: 3 stages', type: 'info', wait: 420 },
            { text: '[Python] Native runtime initialized', type: 'info', wait: 400 },
            { text: '[State] scores[] sanitized and synchronized', type: 'warning', wait: 480 },
            { text: '[Node.js] Payload generated from shared state', type: 'info', wait: 420 },
            { text: '[Orchestrator] Program completed successfully', type: 'success', wait: 0 }
        ];
        let started = false;
        const runTerminal = async () => {
            if (started) return;
            started = true;
            for (const item of lines) {
                const line = document.createElement('div');
                line.className = `term-line ${item.type === 'command' ? '' : item.type}`;
                if (item.type === 'command') {
                    const prompt = document.createElement('span');
                    prompt.className = 'prompt';
                    prompt.textContent = 'root@block:~#';
                    const text = document.createTextNode('');
                    line.append(prompt, text);
                    terminal.appendChild(line);
                    for (const character of item.text) {
                        text.textContent += character;
                        if (!reducedMotion) await new Promise(resolve => setTimeout(resolve, 34));
                    }
                } else {
                    const time = new Date().toLocaleTimeString('en-GB', { hour12: false });
                    const timestamp = document.createElement('span');
                    timestamp.className = 'timestamp';
                    timestamp.textContent = `[${time}]`;
                    line.append(timestamp, document.createTextNode(` ${item.text}`));
                    terminal.appendChild(line);
                }
                if (!reducedMotion && item.wait) await new Promise(resolve => setTimeout(resolve, item.wait));
            }
        };
        if (!('IntersectionObserver' in window)) runTerminal();
        else {
            const terminalObserver = new IntersectionObserver(entries => {
                if (!entries[0].isIntersecting) return;
                runTerminal();
                terminalObserver.disconnect();
            }, { threshold: .35 });
            terminalObserver.observe(terminal);
        }
    }

    const canvas = document.getElementById('bg-canvas');
    const context = canvas?.getContext('2d');
    if (canvas && context && !homePage) {
        let width = 0;
        let height = 0;
        let ratio = 1;
        let pointerX = 0;
        let pointerY = 0;
        let frameId = 0;
        const stars = Array.from({ length: 170 }, (_, index) => ({
            x: ((index * 97) % 997) / 997,
            y: ((index * 193) % 991) / 991,
            z: .2 + (((index * 47) % 100) / 100) * .8,
            phase: ((index * 31) % 628) / 100
        }));
        const resize = () => {
            width = window.innerWidth;
            height = window.innerHeight;
            ratio = Math.min(window.devicePixelRatio || 1, 2);
            canvas.width = Math.round(width * ratio);
            canvas.height = Math.round(height * ratio);
            canvas.style.width = `${width}px`;
            canvas.style.height = `${height}px`;
            context.setTransform(ratio, 0, 0, ratio, 0, 0);
        };
        const draw = (time = 0) => {
            context.clearRect(0, 0, width, height);
            const seconds = time * .001;
            const scrollShift = window.scrollY * .018;
            stars.forEach(star => {
                const x = (star.x * width + pointerX * star.z + width) % width;
                const y = (star.y * height + pointerY * star.z + scrollShift * star.z + height) % height;
                const opacity = .12 + (Math.sin(seconds * (.35 + star.z) + star.phase) + 1) * .13;
                context.beginPath();
                context.arc(x, y, .35 + star.z * .85, 0, Math.PI * 2);
                context.fillStyle = `rgba(255,255,255,${opacity})`;
                context.fill();
            });
            if (!reducedMotion) frameId = requestAnimationFrame(draw);
        };
        resize();
        draw();
        window.addEventListener('resize', resize);
        window.addEventListener('pointermove', event => {
            pointerX = (event.clientX / Math.max(1, width) - .5) * 18;
            pointerY = (event.clientY / Math.max(1, height) - .5) * 12;
        }, { passive: true });
        window.addEventListener('pagehide', () => cancelAnimationFrame(frameId), { once: true });
    }
});
