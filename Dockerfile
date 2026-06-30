ARG NODE_VERSION=26
ARG DEBIAN_VERSION=trixie
FROM node:${NODE_VERSION}-${DEBIAN_VERSION}

ARG DEBIAN_VERSION_NUMBER=13
ARG OPENCODE_VERSION=latest

# set working directory
WORKDIR /app

RUN apt update && \
  apt install -y curl wget zsh git

# install opencode globally
RUN npm i -g "opencode-ai@${OPENCODE_VERSION}" && \
  installed_version_raw="$(opencode --version)" && \
  installed_version="${installed_version_raw#v}" && \
  echo "Installed opencode version: ${installed_version}" && \
  if [ "${OPENCODE_VERSION}" != "latest" ] && [ "${installed_version}" != "${OPENCODE_VERSION}" ]; then \
    echo "Expected opencode version ${OPENCODE_VERSION}, got ${installed_version}" >&2; \
    exit 1; \
  fi

# install corepack and pnpm
RUN npm install -g corepack && \
  corepack enable pnpm

# install dotnet
RUN wget https://packages.microsoft.com/config/debian/${DEBIAN_VERSION_NUMBER}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
  dpkg -i packages-microsoft-prod.deb && \
  rm packages-microsoft-prod.deb && \
  apt-get update && \
  apt-get install -y dotnet-sdk-10.0

# non-root user (recommended)
RUN adduser --disabled-password opencode

# create necessary directories and set permissions
RUN mkdir -p /home/opencode/.local/share/opencode/ && \
  mkdir -p /home/opencode/.local/state/opencode && \
  mkdir -p /home/opencode/.config/opencode/ && \
  chown -R opencode:opencode /home/opencode

# switch to non-root user
USER opencode

CMD ["opencode"]